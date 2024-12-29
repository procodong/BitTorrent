using BitTorrentClient.Files.Streams;
using BitTorrentClient.Models.Messages;
using BitTorrentClient.Torrents.Peers.Errors;
using BitTorrentClient.Utils;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using BitTorrentClient.Torrents.Peers.Encoding;
using BitTorrentClient.Utils.Parsing;

namespace BitTorrentClient.Torrents.Peers.Streaming;
public class PeerWireStream : IDisposable, IAsyncDisposable
{
    private readonly ReadWriteCursoredStream _stream;
    private readonly byte[] _readBuffer;
    private readonly byte[] _writeBuffer;
    private readonly BigEndianBinaryReader _readCursor;
    private readonly BigEndianBinaryWriter _writeCursor;
    public const string PROTOCOL = "BitTorrent protocol";
    private bool _handShaken;
    public bool HandShaken => _handShaken;

    public bool Written => _writeCursor.Position != 0;

    public PeerWireStream(Stream stream)
    {
        _stream = new(stream);
        _readBuffer = new byte[1 << 10];
        _writeBuffer = new byte[1 << 10];
        _writeCursor = new(_writeBuffer);
        _readCursor = new(_readBuffer);
    }

    public async Task SendHandShakeAsync(ZeroCopyBitArray? bitfield, byte[] infoHash, byte[] peerId)
    {
        _handShaken = true;
        MessageEncoder.EncodeHandShake(_writeCursor, new(PROTOCOL, infoHash, peerId));
        if (bitfield is not null)
        {
            MessageEncoder.EncodeHeader(_writeCursor, new(bitfield.Value.Buffer.Length + 1, MessageType.Bitfield));
            await _stream.WriteAsync(_writeCursor.Cursor);
            await _stream.Stream.WriteAsync(bitfield.Value.Buffer);
        }
        else
        {
            await _stream.WriteAsync(_writeCursor.Cursor);
        }
        await _stream.Stream.FlushAsync();
    }

    public async Task<HandShake> ReadHandShakeAsync(CancellationToken cancellationToken = default)
    {
        await _stream.ReadAtLeastAsync(_readCursor.Cursor, MessageDecoder.HANDSHAKE_LEN, cancellationToken: cancellationToken);
        HandShake receivedHandshake = MessageDecoder.DecodeHandShake(_readCursor);
        if (receivedHandshake.Protocol != PROTOCOL)
        {
            throw new BadPeerException(PeerErrorReason.InvalidProtocol);
        }
        return receivedHandshake;
    }

    public void WriteUpdateRelation(Relation relation)
    {
        MessageEncoder.EncodeHeader(_writeCursor, new(1, (MessageType)relation));
    }

    public void WriteKeepAlive()
    {
        _writeCursor.Write(0);
    }

    public void WriteHaveMessage(int piece)
    {
        MessageEncoder.EncodeHeader(_writeCursor, new(5, MessageType.Have));
        _writeCursor.Write(piece);
    }

    public void WritePieceRequest(PieceRequest request)
    {
        MessageEncoder.EncodeHeader(_writeCursor, new(13, MessageType.Request));
        MessageEncoder.EncodePieceRequest(_writeCursor, request);
    }

    public async Task WritePieceAsync(PieceShareHeader requestedPiece, Stream piece, CancellationToken cancellationToken = default)
    {
        MessageEncoder.EncodeHeader(_writeCursor, new((int)piece.Length + 9, MessageType.Piece));
        MessageEncoder.EncodePieceHeader(_writeCursor, requestedPiece);
        while (true)
        {
            int writeLen = await piece.ReadAsync(_writeBuffer.AsMemory(_writeCursor.Position), cancellationToken);
            _writeCursor.Position += writeLen;
            if (_writeCursor.Position == _writeBuffer.Length)
            {
                await _stream.WriteAsync(_writeCursor.Cursor, cancellationToken);
            }
            else if (writeLen == 0)
            {
                break;
            }
        }
        await _stream.WriteAsync(_writeCursor.Cursor, cancellationToken);
    }

    public async Task<Message> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        int len = 0;
        while (len == 0)
        {
            if (_readCursor.Cursor.RemainingBytes < 4)
            {
                await _stream.ReadAsync(_readCursor.Cursor, cancellationToken);
            }
            len = _readCursor.ReadInt32();
        }
        int buffered = _readCursor.Cursor.RemainingBytes;
        if (len < _readBuffer.Length && buffered < len)
        {
            await _stream.ReadAtLeastAsync(_readCursor.Cursor, len - buffered, cancellationToken);
        }
        byte type = _readCursor.ReadByte();
        if (type > (byte)MessageType.Cancel)
        {
            throw new BadPeerException(PeerErrorReason.InvalidProtocol);
        }
        return new(len - 1, (MessageType)type);
    }

    public async Task ReceiveAsync(IPeerEventHandler eventHandler, long maxMessageLength, CancellationToken cancellationToken = default)
    {
        var message = await ReceiveAsync(cancellationToken);
        int buffered = int.Min(_readCursor.Length - _readCursor.Position, message.Length);
        if (message.Length > maxMessageLength)
        {
            throw new BadPeerException(PeerErrorReason.InvalidPacketSize);
        }
        int startPos = _readCursor.Position;
        switch (message.Type)
        {
            case MessageType.Choke:
                await eventHandler.OnChokeAsync(cancellationToken);
                break;
            case MessageType.Unchoke:
                await eventHandler.OnUnchokedAsync(cancellationToken);
                break;
            case MessageType.Interested:
                await eventHandler.OnInterestedAsync(cancellationToken);
                break;
            case MessageType.NotInterested:
                await eventHandler.OnNotInterestedAsync(cancellationToken);
                break;
            case MessageType.Have:
                var index = _readCursor.ReadInt32();
                await eventHandler.OnHaveAsync(index, cancellationToken);
                break;
            case MessageType.Bitfield:
                byte[] buffer = new byte[message.Length];
                _readBuffer.AsSpan(_readCursor.Position, buffered).CopyTo(buffer);
                await _stream.Stream.ReadExactlyAsync(buffer.AsMemory(buffered..), cancellationToken);
                var bitfield = new BitArray(buffer);
                await eventHandler.OnBitfieldAsync(bitfield, cancellationToken);
                break;
            case MessageType.Request:
                var request = MessageDecoder.DecodeRequest(_readCursor);
                await eventHandler.OnRequestAsync(request, cancellationToken);
                break;
            case MessageType.Piece:
                var piece = MessageDecoder.DecodePieceHeader(_readCursor);
                int headerLen = _readCursor.Position - startPos;
                var pieceRequest = new PieceRequest(piece.Index, piece.Begin, message.Length - headerLen);
                int savedCount = buffered - headerLen;
                int streamLen = pieceRequest.Length - savedCount;
                var bufferedData = new MemoryStream(_readBuffer, _readCursor.Position, savedCount);
                var stream = new ConcatStream(bufferedData, new LimitedStream(_stream.Stream, streamLen));
                await eventHandler.OnPieceAsync(new(pieceRequest, stream), cancellationToken);
                break;
            case MessageType.Cancel:
                PieceRequest cancel = MessageDecoder.DecodeRequest(_readCursor);
                await eventHandler.OnCancelAsync(cancel, cancellationToken);
                break;
        }
        _readCursor.Position = startPos + buffered;
    }

    public Task FlushAsync(CancellationToken cancellationToken = default) => _stream.FlushAsync(_writeCursor.Cursor, cancellationToken);

    public void Dispose()
    {
        _stream.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
    }
}
