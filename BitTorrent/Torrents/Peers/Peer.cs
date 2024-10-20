using BitTorrent.Errors;
using BitTorrent.Models.Messages;
using BitTorrent.Models.Peers;
using BitTorrent.Torrents.Downloads;
using BitTorrent.Torrents.Encoding;
using BitTorrent.Utils;
using System.Buffers.Binary;
using System.Collections;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;

namespace BitTorrent.Torrents.Peers;

public class Peer : IDisposable, IAsyncDisposable
{

    private readonly PeerConnection _connection;
    private bool _choked = true;
    private bool _interested = false;
    private bool _amChoking = true;
    private bool _amInterested = false;
    private BitArray _ownedPieces;
    private readonly Download _download;
    private readonly PeerStatistics _stats;
    private long _downloadedPiecesOffset = 0;
    private int? _pieceDownload;

    public async static Task<Peer> ConnectAsync(TcpClient connection, Download download, PeerStatistics stats, string clientId)
    {
        var myHandshake = new HandShake("BitTorrent protocol", download.Torrent.OriginalInfoHashBytes, clientId);
        var (client, handshake) = await PeerConnection.ConnectAsync(connection, myHandshake, download.DownloadedPieces);
        if (handshake.InfoHash != download.Torrent.OriginalInfoHashBytes)
        {
            throw new InvalidInfoHashException(handshake.InfoHash);
        }
        return new(client, download, stats);
    }

    private Peer(PeerConnection connection, Download download, PeerStatistics stats)
    {
        _connection = connection;
        _ownedPieces = new(download.Torrent.NumberOfPieces);
        _download = download;
        _stats = stats;
    }

    private async Task HandleMessageAsync()
    {
        Message message = await _connection.ReceiveAsync();
        switch (message.Type)
        {
            case MessageType.Choke:
                _choked = true;
                break;
            case MessageType.Unchoke:
                _choked = false;
                break;
            case MessageType.Interested:
                _interested = true;
                break;
            case MessageType.NotInterested:
                _interested = false;
                break;
            case MessageType.Have:
                var index = new BigEndianBinaryReader(message.Stream).ReadInt32();
                _ownedPieces[index] = true;
                break;
            case MessageType.Bitfield:
                var bitfield = new byte[message.Stream.Length];
                await message.Stream.ReadExactlyAsync(bitfield);
                _ownedPieces = new BitArray(bitfield);
                break;
            case MessageType.Request:
                if (_amChoking)
                {
                    return;
                }
                var request = MessageDecoder.DecodeRequest(new(message.Stream));
                var block = _download.RequestBlock(request);
                await _connection.SendPieceAsync(new(request.Index, request.Begin), block);
                break;
            case MessageType.Piece:
                var piece = MessageDecoder.DecodePieceHeader(new(message.Stream));
                await _download.SaveBlockAsync(message.Stream, _pieceDownload, piece.Begin);
                var blockLength = message.Stream.Length - 8;
                _stats.IncrementDownloaded(blockLength);
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
            await HandleMessageAsync();
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _connection.DisposeAsync();
    }
}