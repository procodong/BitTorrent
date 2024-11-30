using BitTorrent.Errors;
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
    private readonly BufferedStream _stream;
    private bool _written;
    public const string PROTOCOL = "BitTorrent protocol";

    public bool Written => _written;

    public PeerWireStream(Stream stream)
    {
        _stream = new BufferedStream(stream);
    }

    private BigEndianBinaryReader Reader
    {
        get => new(_stream);
    }

    private BigEndianBinaryWriter Writer
    {
        get => new(_stream);
    }

    private void OnWrite()
    {
        _written = true;
    }

    public async Task InitializeConnectionAsync(BitArray? bitfield, HandShake handShake)
    {
        MessageEncoder.EncodeHandShake(Writer, handShake);
        if (bitfield is not null)
        {
            await WriteBitfieldAsync(bitfield);
        }
        await FlushAsync();
    }

    public async Task<HandShake> ReadHandShakeAsync()
    {
        HandShake receivedHandshake = await MessageDecoder.DecodeHandShakeAsync(Reader);
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
        OnWrite();
    }

    public void WriteUpdateRelation(Relation relation)
    {
        MessageEncoder.EncodeHeader(Writer, new(1, (MessageType)relation));
        OnWrite();
    }

    public void WriteKeepAlive()
    {
        Writer.Write(0);
        OnWrite();
    }

    public void WriteHaveMessage(int piece)
    {
        MessageEncoder.EncodeHeader(Writer, new(5, MessageType.Have));
        Writer.Write(piece);
        OnWrite();
    }

    public void WritePieceRequest(PieceRequest request)
    {
        MessageEncoder.EncodeHeader(Writer, new(13, MessageType.Request));
        MessageEncoder.EncodePieceRequest(Writer, request);
        OnWrite();
    }

    public async Task WritePieceAsync(PieceShareHeader requestedPiece, Stream piece)
    {
        MessageEncoder.EncodeHeader(Writer, new((int)piece.Length + 9, MessageType.Piece));
        MessageEncoder.EncodePieceHeader(Writer, requestedPiece);
        await piece.CopyToAsync(_stream);
        OnWrite();
    }

    public async Task<Message> ReceiveAsync()
    {
        int len = 0;
        while (len == 0)
        {
            len = await Reader.ReadInt32Async();
        }
        byte type = Reader.ReadByte();
        if (type > (byte)MessageType.Port)
        {
            throw new BadPeerException(PeerErrorReason.InvalidProtocol);
        }
        return new((MessageType)type, new(_stream, len - 1));
    }


    public async Task ReceiveAsync(IPeerEventHandler eventHandler, long maxMessageLength)
    {
        var message = await ReceiveAsync();
        if (message.Stream.Length > maxMessageLength)
        {
            throw new BadPeerException(PeerErrorReason.InvalidPacketSize);
        }
        var parser = new BigEndianBinaryReader(message.Stream);
        switch (message.Type)
        {
            case MessageType.Choke:
                await eventHandler.OnChokeAsync();
                break;
            case MessageType.Unchoke:
                await eventHandler.OnUnchokedAsync();
                break;
            case MessageType.Interested:
                await eventHandler.OnInterestedAsync();
                break;
            case MessageType.NotInterested:
                await eventHandler.OnNotInterestedAsync();
                break;
            case MessageType.Have:
                var index = await Reader.ReadInt32Async();
                await eventHandler.OnHaveAsync(index);
                break;
            case MessageType.Bitfield:
                var bytes = new byte[message.Stream.Length];
                await message.Stream.ReadExactlyAsync(bytes);
                var bitfield = new BitArray(bytes);
                await eventHandler.OnBitfieldAsync(bitfield);
                break;
            case MessageType.Request:
                var request = await MessageDecoder.DecodeRequestAsync(parser);
                await eventHandler.OnRequestAsync(request);
                break;
            case MessageType.Piece:
                var piece = await MessageDecoder.DecodePieceHeaderAsync(parser);
                var pieceRequest = new PieceRequest(piece.Index, piece.Begin, (int)(message.Stream.Length - message.Stream.Position));
                await eventHandler.OnPieceAsync(new(pieceRequest, message.Stream));
                break;
            case MessageType.Cancel:
                PieceRequest cancel = await MessageDecoder.DecodeRequestAsync(parser);
                await eventHandler.OnCancelAsync(cancel);
                break;
            case MessageType.Port:
                await eventHandler.OnPortAsync(parser.ReadUInt16());
                break;
        }
        if (message.Stream.Position != message.Stream.Length)
        {
            throw new BadPeerException(PeerErrorReason.InvalidPacketSize);
        }
    }

    public async Task FlushAsync()
    {
        if (_written)
        {
            await _stream.FlushAsync();
            _written = false;
        }
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
