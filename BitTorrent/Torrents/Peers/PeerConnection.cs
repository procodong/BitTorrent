using BitTorrent.Errors;
using BitTorrent.Models.Messages;
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
public class PeerConnection : IDisposable, IAsyncDisposable
{
    private readonly BufferedStream _stream;
    private readonly TcpClient _client;

    public PeerConnection(TcpClient connection)
    {
        _stream = new BufferedStream(connection.GetStream());
        connection.ReceiveTimeout = 2 * 60 * 1000;
        _client = connection;
    }

    private PeerConnection(BufferedStream stream, TcpClient client)
    {
        _stream = stream;
        _client = client;
    }
    private BigEndianBinaryReader Reader
    {
        get => new(_stream);
    }

    private BigEndianBinaryWriter Writer
    {
        get => new(_stream);
    }

    public async Task<HandShake> HandShakeAsync(HandShake handShake)
    {
        WriteHandshake(handShake);
        HandShake receivedHandshake = await MessageDecoder.DecodeHandShakeAsync(Reader);
        if (receivedHandshake.Protocol != handShake.Protocol)
        {
            throw new BadPeerException(PeerErrorReason.InvalidProtocol);
        }
        return receivedHandshake;
    }

    public void WriteHandshake(HandShake handShake)
    {
        MessageEncoder.EncodeHandShake(Writer, handShake);
    }

    public async Task WriteBitFieldAsync(BitArray bitfield)
    {
        int len = bitfield.Length * 8;
        var buf = new byte[len];
        bitfield.CopyTo(buf, 0);
        MessageEncoder.EncodeHeader(Writer, new(len + 1, MessageType.Bitfield));
        await _stream.WriteAsync(buf);
    }

    public void WriteUpdateRelation(Relation relation)
    {
        MessageEncoder.EncodeHeader(Writer, new(1, (MessageType)relation));
    }

    public async Task KeepAliveAsync()
    {
        Writer.Write(0);
        await _stream.FlushAsync();
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

    public async Task WritePieceAsync(PieceShareHeader requestedPiece, Stream piece)
    {
        MessageEncoder.EncodeHeader(Writer, new((int)piece.Length + 9, MessageType.Piece));
        MessageEncoder.EncodePieceHeader(Writer, requestedPiece);
        await piece.CopyToAsync(_stream);
    }

    public async Task<Message> ReceiveAsync()
    {
        int len =  await Reader.ReadInt32Async();
        if (len == 0)
        {
            return new(MessageType.KeepAlive, new(_stream, 0));
        }
        byte type = Reader.ReadByte();
        if (type > (byte)MessageType.Port)
        {
            throw new BadPeerException(PeerErrorReason.InvalidProtocol);
        }
        return new((MessageType)type, new(_stream, len - 1));
    }

    public async Task FlushAsync()
    {
        await _stream.FlushAsync();
    }

    public void Dispose()
    {
        _stream.Dispose();
        _client.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _stream.DisposeAsync();
    }
}
