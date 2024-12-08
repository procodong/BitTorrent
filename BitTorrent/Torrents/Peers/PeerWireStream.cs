using BitTorrent.Files.Streams;
using BitTorrent.Models.Messages;
using BitTorrent.Torrents.Downloads;
using BitTorrent.Torrents.Encoding;
using BitTorrent.Torrents.Peers.Errors;
using BitTorrent.Utils;
using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Peers;
public class PeerWireStream : IDisposable, IAsyncDisposable
{
    private readonly Stream _stream;
    private readonly byte[] _readBuffer;
    private readonly byte[] _writeBuffer;
    private MemoryStream _readCursor;
    private readonly MemoryStream _writeCursor;
    public const string PROTOCOL = "BitTorrent protocol";
    private bool _handShaken;
    public bool HandShaken => _handShaken;

    public bool Written => _writeCursor.Position != 0;

    private BigEndianBinaryReader Reader => new(_readCursor);
    private BigEndianBinaryWriter Writer => new(_writeCursor);

    public PeerWireStream(Stream stream)
    {
        _stream = stream;
        _readBuffer = new byte[1 << 9];
        _writeBuffer = new byte[1 << 13];
        _writeCursor = new(_writeBuffer);
        _readCursor = new(_readBuffer);
    }

    public async Task SendHandShake(BitArray? bitfield, byte[] infoHash, string peerId)
    {
        _handShaken = true;
        MessageEncoder.EncodeHandShake(Writer, new(PROTOCOL, infoHash, peerId));
        if (bitfield is not null)
        {
            await WriteBitfieldAsync(bitfield);
        }
        await FlushAsync();
    }

    public async Task<HandShake> ReadHandShakeAsync()
    {
        await _stream.ReadAtLeastAsync(_readBuffer, MessageDecoder.HANDSHAKE_LEN);
        HandShake receivedHandshake = MessageDecoder.DecodeHandShake(Reader);
        if (!receivedHandshake.Protocol.SequenceEqual(PROTOCOL))
        {
            throw new BadPeerException(PeerErrorReason.InvalidProtocol);
        }
        return receivedHandshake;
    }

    public async Task WriteBitfieldAsync(BitArray bitfield)
    {
        int len = bitfield.Length / 8;
        if (bitfield.Length % 8 != 0)
        {
            len++;
        }
        var buf = new byte[len];
        bitfield.CopyTo(buf, 0);
        MessageEncoder.EncodeHeader(Writer, new(len + 1, MessageType.Bitfield));
        await _stream.WriteAsync(buf);
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
            int writeLen = await piece.ReadAsync(_writeBuffer.AsMemory((int)_writeCursor.Position), cancellationToken);
            if (writeLen == 0) break;
            _writeCursor.Position += writeLen;
            if (_writeCursor.Position == _writeBuffer.Length)
            {
                await FlushAsync();
            }
        }
    }

    private async Task ReadAsync(CancellationToken cancellationToken = default)
    {
        int readLen = await _stream.ReadAsync(_readBuffer, cancellationToken);
        if (readLen == 0)
        {
            throw new EndOfStreamException();
        }
        _readCursor = new MemoryStream(_readBuffer, 0, readLen);
    }

    public async Task<Message> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        int len = 0;
        while (len == 0)
        {
            try
            {
                len = Reader.ReadInt32();
            }
            catch (EndOfStreamException)
            {
                await ReadAsync(cancellationToken);
            }
        }
        byte type = Reader.ReadByte();
        if (type > (byte)MessageType.Port)
        {
            throw new BadPeerException(PeerErrorReason.InvalidProtocol);
        }
        return new(len - 1, (MessageType)type);
    }


    public async Task ReceiveAsync(IPeerEventHandler eventHandler, long maxMessageLength, CancellationToken cancellationToken = default)
    {
        var message = await ReceiveAsync(cancellationToken);
        if (message.Length > maxMessageLength)
        {
            throw new BadPeerException(PeerErrorReason.InvalidPacketSize);
        }
        int buffered = int.Min((int)_readCursor.Length - (int)_readCursor.Position, message.Length);
        long startPos = _readCursor.Position;
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
                _readCursor.Read(buffer.AsSpan(..buffered));
                await _stream.ReadExactlyAsync(buffer.AsMemory(buffered), cancellationToken);
                var bitfield = new BitArray(buffer);
                await eventHandler.OnBitfieldAsync(bitfield, cancellationToken);
                break;
            case MessageType.Request:
                var request = MessageDecoder.DecodeRequest(Reader);
                await eventHandler.OnRequestAsync(request, cancellationToken);
                break;
            case MessageType.Piece:
                var piece = MessageDecoder.DecodePieceHeader(Reader);
                var pieceRequest = new PieceRequest(piece.Index, piece.Begin, message.Length - MessageDecoder.PIECE_HEADER_LEN);
                int savedCount = int.Min((int)_readCursor.Length - (int)_readCursor.Position, pieceRequest.Length);
                int streamLen = pieceRequest.Length - savedCount;
                var stream = new ConcatStream(new LimitedStream(_readCursor, savedCount), new LimitedStream(_stream, streamLen));
                await eventHandler.OnPieceAsync(new(pieceRequest, stream), cancellationToken);
                break;
            case MessageType.Cancel:
                PieceRequest cancel = MessageDecoder.DecodeRequest(Reader);
                await eventHandler.OnCancelAsync(cancel, cancellationToken);
                break;
            case MessageType.Port:
                await eventHandler.OnPortAsync(Reader.ReadUInt16(), cancellationToken);
                break;
        }
        if (_readCursor.Position - startPos != buffered)
        {
            _readCursor.Position = startPos + buffered;
        }
    }


    public async Task FlushAsync()
    {
        await _stream.WriteAsync(_writeBuffer.AsMemory(..(int)Writer.BaseStream.Position));
        await _stream.FlushAsync();
        _writeCursor.Position = 0;
    }

    public void Dispose()
    {
        _stream.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
    }
}
