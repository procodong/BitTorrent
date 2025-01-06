using BitTorrentClient.Files.Streams;
using BitTorrentClient.Models.Messages;
using BitTorrentClient.BitTorrent.Peers.Errors;
using BitTorrentClient.Utils;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using BitTorrentClient.Utils.Parsing;
using System.Buffers;
using BitTorrentClient.BitTorrent.Peers.Parsing;
using BitTorrentClient.BitTorrent.Peers.Connections;

namespace BitTorrentClient.BitTorrent.Peers.Streaming;
public class PeerWireStream : IDisposable, IAsyncDisposable
{
    private readonly BufferCursoredStream _stream;
    private readonly BufferCursor _readCursor;
    private readonly BufferCursor _writeCursor;
    public const string PROTOCOL = "BitTorrent protocol";
    private bool _handShaken;
    public bool HandShaken => _handShaken;
    public bool Written => _writeCursor.Position != 0;

    private BigEndianBinaryReader Reader => new(_readCursor);
    private BigEndianBinaryWriter Writer => new(_writeCursor);

    public PeerWireStream(Stream stream)
    {
        _stream = new(stream);
        _writeCursor = new(new byte[1 << 10]);
        _readCursor = new(new byte[1 << 10], 0, 0);
    }

    public async Task SendHandShakeAsync(ZeroCopyBitArray? bitfield, byte[] infoHash, byte[] peerId)
    {
        _handShaken = true;
        MessageEncoder.EncodeHandShake(Writer, new(PROTOCOL, infoHash, peerId));
        if (bitfield is not null)
        {
            MessageEncoder.EncodeHeader(Writer, new(bitfield.Value.Buffer.Length + 1, MessageType.Bitfield));
            await _stream.WriteAsync(_writeCursor);
            await _stream.UnderlyingStream.WriteAsync(bitfield.Value.Buffer);
        }
        else
        {
            await _stream.WriteAsync(_writeCursor);
        }
        await _stream.UnderlyingStream.FlushAsync();
    }

    public async Task<HandShake> ReadHandShakeAsync(CancellationToken cancellationToken = default)
    {
        await _stream.ReadAtLeastAsync(_readCursor, MessageDecoder.HANDSHAKE_LEN, cancellationToken: cancellationToken);
        HandShake receivedHandshake = MessageDecoder.DecodeHandShake(Reader);
        if (receivedHandshake.Protocol != PROTOCOL)
        {
            throw new BadPeerException(PeerErrorReason.InvalidProtocol);
        }
        return receivedHandshake;
    }

    public void WriteUpdateRelation(Relation relation)
    {
        MessageEncoder.EncodeHeader(Writer, new(1, (MessageType)relation));
    }

    public void WriteKeepAlive()
    {
        Writer.Write(0);
    }

    public void WriteHaveMessage(int piece)
    {
        MessageEncoder.EncodeHeader(Writer, new(5, MessageType.Have));
        Writer.Write(piece);
    }

    public void WritePieceRequest(PieceRequest request)
    {
        MessageEncoder.EncodeHeader(Writer, new(13, MessageType.Request));
        MessageEncoder.EncodePieceRequest(Writer, request);
    }

    public async Task WritePieceAsync(PieceShareHeader requestedPiece, Stream piece, CancellationToken cancellationToken = default)
    {
        MessageEncoder.EncodeHeader(Writer, new((int)piece.Length + 9, MessageType.Piece));
        MessageEncoder.EncodePieceHeader(Writer, requestedPiece);
        while (true)
        {
            int writeLen = await piece.ReadAsync(_writeCursor.Buffer.AsMemory(_writeCursor.Position), cancellationToken);
            _writeCursor.Position += writeLen;
            if (_writeCursor.Position == _writeCursor.Buffer.Length)
            {
                await _stream.WriteAsync(_writeCursor, cancellationToken);
            }
            else if (writeLen == 0)
            {
                break;
            }
        }
        await _stream.WriteAsync(_writeCursor, cancellationToken);
    }

    public async Task<Message> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        int len = 0;
        while (len == 0)
        {
            if (_readCursor.RemainingBytes < 4)
            {
                await _stream.ReadAsync(_readCursor, cancellationToken);
            }
            len = Reader.ReadInt32();
        }
        int buffered = _readCursor.RemainingBytes;
        if (len < _writeCursor.Length && buffered < len)
        {
            await _stream.ReadAtLeastAsync(_readCursor, len - buffered, cancellationToken);
        }
        byte type = Reader.ReadByte();
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
                var index = Reader.ReadInt32();
                await eventHandler.OnHaveAsync(index, cancellationToken);
                break;
            case MessageType.Bitfield:
                byte[] buffer = new byte[message.Length];
                _writeCursor.Buffer.AsSpan(_readCursor.Position, buffered).CopyTo(buffer);
                await _stream.UnderlyingStream.ReadExactlyAsync(buffer.AsMemory(buffered..), cancellationToken);
                var bitfield = new BitArray(buffer);
                await eventHandler.OnBitfieldAsync(bitfield, cancellationToken);
                break;
            case MessageType.Request:
                var request = MessageDecoder.DecodeRequest(Reader);
                await eventHandler.OnRequestAsync(request, cancellationToken);
                break;
            case MessageType.Piece:
                var piece = MessageDecoder.DecodePieceHeader(Reader);
                int headerLen = _readCursor.Position - startPos;
                var pieceRequest = new PieceRequest(piece.Index, piece.Begin, message.Length - headerLen);
                int savedCount = buffered - headerLen;
                int streamLen = pieceRequest.Length - savedCount;
                var bufferedData = new MemoryStream(_writeCursor.Buffer, _readCursor.Position, savedCount);
                var stream = new ConcatStream(bufferedData, new LimitedStream(_stream.UnderlyingStream, streamLen));
                await eventHandler.OnPieceAsync(new(pieceRequest, stream), cancellationToken);
                break;
            case MessageType.Cancel:
                PieceRequest cancel = MessageDecoder.DecodeRequest(Reader);
                await eventHandler.OnCancelAsync(cancel, cancellationToken);
                break;
        }
        _readCursor.Position = startPos + buffered;
    }

    public Task FlushAsync(CancellationToken cancellationToken = default) => _stream.FlushAsync(_writeCursor, cancellationToken);

    public void Dispose()
    {
        _stream.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
    }
}
