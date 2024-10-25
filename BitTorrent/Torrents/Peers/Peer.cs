using BitTorrent.Errors;
using BitTorrent.Models.Messages;
using BitTorrent.Models.Peers;
using BitTorrent.Torrents.Downloads;
using BitTorrent.Torrents.Encoding;
using BitTorrent.Torrents.Peers.Errors;
using BitTorrent.Utils;
using System.Buffers.Binary;
using System.Collections;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;

namespace BitTorrent.Torrents.Peers;

public class Peer : IDisposable, IAsyncDisposable
{

    private readonly PeerConnection _connection;
    private readonly Download _download;
    private readonly PeerStatistics _stats;
    private readonly ChannelReader<int> _haveMessages;
    private readonly List<QueuedPieceRequest> _pieceDownloads = [];
    private BitArray _ownedPieces;
    private bool _choked = true;
    private bool _interested = false;
    private bool _amChoking = true;
    private bool _amInterested = false;
    private int _downloadedPiecesOffset = 0;

    public async static Task<Peer> ConnectAsync(TcpClient connection, ChannelReader<int> haveMessages, Download download, PeerStatistics stats, string clientId)
    {
        var client = new PeerConnection(connection);
        var myHandshake = new HandShake("BitTorrent protocol", download.Torrent.OriginalInfoHashBytes, clientId);
        HandShake handshake = await client.HandShakeAsync(myHandshake);
        if (handshake.InfoHash != download.Torrent.OriginalInfoHashBytes)
        {
            throw new BadPeerException(PeerErrorReason.InvalidInfoHash);
        }
        if (download.DownloadedPiecesCount != 0)
        {
            await client.WriteBitFieldAsync(download.DownloadedPieces);
        }
        var peer = new Peer(client, haveMessages, download, stats);
        if (!download.FinishedDownloading)
        {
            peer.UpdateRelation(Relation.Interested);
        }
        await client.FlushAsync();
        return peer;
    }

    private Peer(PeerConnection connection, ChannelReader<int> haveMessages, Download download, PeerStatistics stats)
    {
        _connection = connection;
        _ownedPieces = new(download.Torrent.NumberOfPieces);
        _download = download;
        _stats = stats;
        _haveMessages = haveMessages;
    }

    private async Task HandleMessageAsync()
    {
        Message message = await _connection.ReceiveAsync();
        if (message.Stream.Length > _download.MaxMessageLength)
        {
            throw new BadPeerException(PeerErrorReason.InvalidPacketSize);
        }
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
                HandleBitfield(new BitArray(bitfield));
                break;
            case MessageType.Request:
                if (_amChoking)
                {
                    return;
                }
                var request = MessageDecoder.DecodeRequest(new(message.Stream));
                await HandleRequestAsync(request);
                break;
            case MessageType.Piece:
                var piece = MessageDecoder.DecodePieceHeader(new(message.Stream));
                await HandlePieceAsync(message.Stream, piece);
                break;
            case MessageType.Cancel:
                PieceRequest cancel = MessageDecoder.DecodeRequest(new(message.Stream));
                _download.Cancel(cancel, FindDownload(cancel.Index).Download);
                _pieceDownloads.RemoveAll(download => download.Download.PieceIndex == cancel.Index);
                break;
            case MessageType.Port:
                message.Stream.ReadByte();
                message.Stream.ReadByte();
                break;
        }
        if (message.Stream.Position != message.Stream.Length)
        {
            throw new BadPeerException(PeerErrorReason.InvalidPacketSize);
        }
    }

    private QueuedPieceRequest FindDownload(int pieceIndex)
    {
        return _pieceDownloads.Find(d => d.Download.PieceIndex == pieceIndex);
    }

    private async Task HandlePieceAsync(Stream stream, PieceShare piece)
    {
        QueuedPieceRequest download = FindDownload(piece.Index);
        await _download.SaveBlockAsync(stream, download.Download, piece.Begin);
        long blockLength = stream.Length - 8;
        
        _stats.IncrementDownloaded(blockLength);
    }

    private void HandleHave(int pieceIndex)
    {
        _ownedPieces[pieceIndex] = true;
        if (_downloadedPiecesOffset + 1 == pieceIndex)
        {
            _downloadedPiecesOffset++;
        }
    }

    private async Task HandleRequestAsync(PieceRequest request)
    {
        var block = _download.RequestBlock(request);
        await _connection.WritePieceAsync(new(request.Index, request.Begin), block);
        await _connection.FlushAsync();
    }

    private void HandleBitfield(BitArray bitfield)
    {
        _ownedPieces = bitfield;
        int finishedOffset = 0;
        while (_ownedPieces[finishedOffset])
        {
            finishedOffset++;
        }
        _downloadedPiecesOffset = finishedOffset;
    }

    public void UpdateRelation(Relation relation)
    {
        _ = relation switch
        {
            Relation.Choke => _amChoking = true,
            Relation.Unchoke => _amChoking = false,
            Relation.Interested => _amInterested = true,
            Relation.NotInterested => _amInterested = false,
            _ => throw new NotImplementedException()
        };
        _connection.WriteUpdateRelation(relation);
    }

    private void RequestBlocks()
    {
        if (_choked || !_amInterested)
        {
            return;
        }
        while (_pieceDownloads.Count < _download.Config.RequestQueueSize)
        {
            PieceRequest? request = default; 
            lock (_download)
            {
                request = _download.AssignBlockRequest(_ownedPieces, _downloadedPiecesOffset);
            }
            if (!request.HasValue)
            {
                break;
            }
            _connection.WritePieceRequest(request.Value);
        }
    }

    private void ReadHaveMessages()
    {
        while (_haveMessages.TryRead(out var message))
        {
            _connection.WriteHaveMessage(message);
        }
    }

    public async Task Listen()
    {
        while (true)
        {
            ReadHaveMessages();
            RequestBlocks();
            await _connection.FlushAsync();
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