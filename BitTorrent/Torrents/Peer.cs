using BitTorrent.Errors;
using BitTorrent.Models;
using System.Buffers.Binary;
using System.Collections;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;

namespace BitTorrent.Torrents;

public class Peer : IDisposable
{
    private const int REQUEST_SIZE = 1 << 14;

    private readonly TcpClient client;
    private bool interested = false;
    private bool choked = true;
    private bool amChoking = true;
    private bool amInterested = true;
    private readonly byte[] buffer;
    private BitArray ownedPieces;
    private readonly Download download;
    private Range requestingPieces = 0..0;
    private int requestedPieceIndex = 0;
    private int pieceOffset = 0;
    private readonly ChannelReader<PeerManagerEvent> events;
    private readonly PeerStatistics stats;


    public async static Task<Peer> Connect(Models.Peer peerData, Download download, ChannelReader<PeerManagerEvent> events, PeerStatistics stats)
    {
        var client = new TcpClient();
        await client.ConnectAsync(peerData.Ip, peerData.Port);
        return new(client, download, events, stats);
    }

    private Peer(TcpClient tcpClient, Download download, ChannelReader<PeerManagerEvent> events, PeerStatistics stats)
    {
        tcpClient.ReceiveTimeout = 2 * 60 * 1000;
        buffer = new byte[REQUEST_SIZE + 9];
        client = tcpClient;
        ownedPieces = new(download.Torrent.NumberOfPieces);
        this.download = download;
        this.events = events;
        this.stats = stats;
    }

    public async Task Send(MessageType type, ReadOnlyMemory<byte> data)
    {
        await Write(type, data);
        await client.GetStream().FlushAsync();
    }

    public async Task Write(MessageType type, ReadOnlyMemory<byte> data)
    {
        var header = new byte[5];
        MessageEncoder.EncodeHeader(header, new(data.Length + 1, type));
        var stream = client.GetStream();
        await stream.WriteAsync(header);
        if (!data.IsEmpty)
        {
            await stream.WriteAsync(data);
        }
    }

    public async Task SendBitfield()
    {
        await Send(MessageType.Bitfield, download.DownloadedPieces.Bytes);
    }

    public async Task RequestBlock()
    {
        var newOffset = pieceOffset + REQUEST_SIZE;
        if (newOffset > download.Torrent.PieceSize)
        {
            pieceOffset = newOffset % (int)download.Torrent.PieceSize;
            requestedPieceIndex++;
        }
        if (requestedPieceIndex == requestingPieces.End.Value)
        {
            lock (download.NotDownloadedPieces)
            {
                if (download.NotDownloadedPieces.Count == 0)
                {
                    return;
                } 
                requestingPieces = download.AssignPieces(ownedPieces);
            }
            requestedPieceIndex = requestingPieces.Start.Value;
            pieceOffset = 0;
        }
        MessageEncoder.EncodePieceRequest(buffer, new(requestedPieceIndex, pieceOffset, REQUEST_SIZE));
        await Send(MessageType.Request, buffer.AsMemory(..12));
    }

    public async Task HandShake(HandShake handShake)
    {
        MessageEncoder.EncodeHandShake(buffer, handShake);
        var stream = client.GetStream();
        await stream.WriteAsync(buffer);
        await stream.FlushAsync();
    }

    public async Task<HandShake> ReceiveHandShake()
    {
        const int HANDSHAKE_SIZE = 68;
        await client.GetStream().ReadExactlyAsync(buffer.AsMemory(..HANDSHAKE_SIZE));
        return MessageDecoder.DecodeHandShake(buffer);
    }

    public async Task<Message> Receive()
    {
        var stream = client.GetStream();
        await stream.ReadExactlyAsync(buffer.AsMemory(..sizeof(int)));
        var len = BinaryPrimitives.ReadInt32BigEndian(buffer);
        if (len >= buffer.Length)
        {
            throw new InvalidPacketSizeException();
        }
        if (len == 0)
        {
            return await Receive();
        }
        await stream.ReadExactlyAsync(buffer.AsMemory(..len));
        var data = buffer.AsMemory(1..len);
        var type = (MessageType)buffer[0];
        return new(type, data);
    }

    public async Task Start(string peerId)
    {
        var protocol = "BitTorrent protocol";
        var handshake = new HandShake(protocol, download.Torrent.OriginalInfoHashBytes, peerId);
        await HandShake(handshake);
        await SendBitfield();
        var receivedHandshake = await ReceiveHandShake();
        if (receivedHandshake.Protocol != handshake.Protocol)
        {
            throw new InvalidProtocolException(receivedHandshake.Protocol);
        }
        if (receivedHandshake.InfoHash != download.Torrent.OriginalInfoHashBytes)
        {
            throw new InvalidInfoHashException(receivedHandshake.InfoHash);
        }
        await Listen();
    }

    private async Task HandleEvents()
    {
        while (events.TryRead(out var managerEvent))
        {
            switch (managerEvent)
            {
                case PeerManagerEvent.Choked:
                    await Send(MessageType.Choke, Array.Empty<byte>());
                    amChoking = true;
                    break;
                case PeerManagerEvent.Unchoked:
                    await Send(MessageType.Unchoke, Array.Empty<byte>());
                    amChoking = false;
                    break;
            }
        }
    }

    private async Task HandleMessage()
    {
        var message = await Receive();
        switch (message.Type)
        {
            case MessageType.Choke:
                choked = true;
                break;
            case MessageType.Unchoke:
                choked = false;
                break;
            case MessageType.Interested:
                interested = true;
                break;
            case MessageType.NotInterested:
                interested = false;
                break;
            case MessageType.Have:
                var index = BinaryPrimitives.ReadInt32BigEndian(message.Data.Span);
                ownedPieces[index] = true;
                break;
            case MessageType.Bitfield:
                ownedPieces = new BitArray(message.Data.ToArray());
                break;
            case MessageType.Request:
                if (amChoking)
                {
                    return;
                }
                var request = MessageDecoder.DecodeRequest(message.Data.Span);
                MessageEncoder.EncodePieceHeader(buffer, new(request.Index, request.Begin));
                await Write(MessageType.Piece, buffer.AsMemory(..8));
                var stream = client.GetStream();
                await download.Files.Read(stream, request.Length, request.Index, request.Begin);
                await stream.FlushAsync();
                break;
            case MessageType.Piece:
                var piece = MessageDecoder.DecodePiece(message.Data);
                await download.Files.Write(piece.Block, piece.Index, piece.Begin);
                download.Statistics.IncrementDownloaded((ulong)piece.Block.Length);
                stats.IncrementDownloaded((ulong)piece.Block.Length);
                await RequestBlock();
                break;
            case MessageType.Cancel:
                break;
            case MessageType.Port:
                break;
        }
    }

    public async Task Listen()
    {
        while (true)
        {
            await HandleEvents();
            await HandleMessage();
        }
    }

    public void Dispose()
    {
        client.Dispose();
    }
}