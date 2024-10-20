using BitTorrent.Errors;
using BitTorrent.Models.Messages;
using BitTorrent.Torrents.Encoding;
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

    public static async Task<(PeerConnection, HandShake)> ConnectAsync(TcpClient connection, HandShake handShake)
    {
        var stream = new BufferedStream(connection.GetStream());
        var peerConn = new PeerConnection(stream, connection);
        MessageEncoder.EncodeHandShake(peerConn.Writer, handShake);
        await stream.FlushAsync();
        HandShake receivedHandshake = await MessageDecoder.DecodeHandShakeAsync(peerConn.Reader);
        if (receivedHandshake.Protocol != handShake.Protocol)
        {
            throw new InvalidProtocolException(receivedHandshake.Protocol);
        }
        return (peerConn, receivedHandshake);
    }

    public static async Task<(PeerConnection, HandShake)> ConnectAsync(TcpClient connection, HandShake handShake, BitArray bitfield)
    {
        var (conn, handshake) = await ConnectAsync(connection, handShake);
        int len = bitfield.Length * 8;
        var buf = new byte[len];
        bitfield.CopyTo(buf, 0);
        MessageEncoder.EncodeHeader(conn.Writer, new(len + 1, MessageType.Bitfield));
        await conn._stream.WriteAsync(buf);
        await conn._stream.FlushAsync();
        return (conn, handshake);
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

    public async Task UpdateRelationAsync(Relation relation)
    {
        MessageEncoder.EncodeHeader(Writer, new(1, (MessageType)relation));
        await _stream.FlushAsync();
    }

    public async Task KeepAliveAsync()
    {
        Writer.Write(0);
        await _stream.FlushAsync();
    }

    public async Task NotifyHaveAsync(int piece)
    {
        MessageEncoder.EncodeHeader(Writer, new(5, MessageType.Have));
        Writer.Write(piece);
        await _stream.FlushAsync();
    }

    public async Task RequestAsync(PieceRequest request)
    {
        MessageEncoder.EncodeHeader(Writer, new(13, MessageType.Request));
        MessageEncoder.EncodePieceRequest(Writer, request);
        await _stream.FlushAsync();
    }

    public async Task SendPieceAsync(PieceShareHeader requestedPiece, Stream piece)
    {
        MessageEncoder.EncodeHeader(Writer, new((int)piece.Length + 9, MessageType.Piece));
        MessageEncoder.EncodePieceHeader(Writer, requestedPiece);
        await piece.CopyToAsync(_stream);
        await _stream.FlushAsync();
    }

    public async Task<Message> ReceiveAsync()
    {
        int len = 0;
        while (len == 0)  // messages with length 0 are keep alive messages and are ignored
        {
            len = await Reader.ReadInt32Async();
        }
        byte type = Reader.ReadByte();
        if (type > (byte)MessageType.Port)
        {
            throw new BadPeerException();
        }
        return new((MessageType)type, new(_stream, len - 1));
    }

    public void Dispose()
    {
        _stream.Dispose();
        _client.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return ((IAsyncDisposable)_stream).DisposeAsync();
    }
}
