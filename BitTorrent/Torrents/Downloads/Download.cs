
using BencodeNET.Torrents;
using BitTorrent.Errors;
using BitTorrent.Files.DownloadFiles;
using BitTorrent.Models.Application;
using BitTorrent.Models.Messages;
using BitTorrent.Models.Peers;
using BitTorrent.Torrents.Downloads.Errors;
using BitTorrent.Torrents.Peers.Errors;
using BitTorrent.Utils;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Downloads;
public class Download(Torrent torrent, DownloadFileManager files, Config config)
{
    public readonly Torrent Torrent = torrent;
    public readonly Config Config = config;
    public readonly BitArray DownloadedPieces = new(torrent.NumberOfPieces);
    public readonly int MaxMessageLength = int.Max(config.RequestSize + 13, torrent.NumberOfPieces / 8 + 1);
    private readonly PeerStatistics _statistics = new();
    private readonly DownloadFileManager _files = files;
    private readonly List<PieceDownload> _pieceRegisters = [];
    private readonly List<int> _rarestPieces = [];
    private readonly BitArray _requestedPieces = new(torrent.NumberOfPieces);
    private readonly List<ChannelWriter<int>> _haveNotificationChannels = [];
    private int _downloadedPieces = 0;

    public bool FinishedDownloading => _downloadedPieces >= Torrent.NumberOfPieces;
    public int DownloadedPiecesCount => _downloadedPieces;


    public async Task SaveBlockAsync(Stream stream, PieceDownload download, int offset)
    {
        int length = (int)(stream.Length - stream.Position);
        var files = _files.GetStream(download.PieceIndex, offset, length);
        byte[] buffer = new byte[length];
        int readBytes;
        int readOffset = 0;
        while ((readBytes = await stream.ReadAsync(buffer)) != 0)
        {
            await files.WriteAsync(buffer.AsMemory(readOffset, readBytes));
            readOffset += readBytes;
        }
        lock (download.Hasher)
        {
            download.Hasher.Hash(buffer, offset);
        }
        int newDownloaded = Interlocked.Add(ref download.Downloaded, length);
        if (newDownloaded < download.Size || download.Size != PieceSize(download.PieceIndex))
        {
            return;
        }
        Interlocked.Increment(ref _downloadedPieces);
        Memory<byte> correctHash = Torrent.Pieces.AsMemory(download.PieceIndex * 20, 20);
        IEnumerable<byte> correctHashIter = MemoryMarshal.ToEnumerable<byte>(correctHash);
        if (!download.Hasher.Finish().SequenceEqual(correctHashIter))
        {
            throw new BadPeerException(PeerErrorReason.InvalidPiece);
        }
        DownloadedPieces[download.PieceIndex] = true;
        foreach (var peer in _haveNotificationChannels)
        {
            await peer.WriteAsync(download.PieceIndex);
        }
    }

    public Stream RequestBlock(PieceRequest request)
    {
        ValidateRequest(request);
        return _files.GetStream(request.Index, request.Begin, request.Length);
    }

    public void Cancel(PieceRequest cancel, PieceDownload download)
    {
        ValidateRequest(cancel);
        var queued = new PieceDownload(cancel.Length, download.PieceIndex, new(Config.RequestSize), cancel.Begin);
        _pieceRegisters.Add(queued);
    }

    private void ValidateRequest(PieceRequest request)
    {
        int size = PieceSize(request.Index);
        if (request.Begin >= size || request.Length > size)
        {
            throw new BadPeerException(PeerErrorReason.InvalidRequest);
        }
    }

    public PieceRequest AllocateDownload(PieceDownload download, int index)
    {
        int downloadSize = int.Min(download.Size - download.Downloading, Config.RequestSize);
        int downloadOffset = download.Downloading;
        download.Downloading += downloadSize;
        if (download.Downloading == download.Size)
        {
            _pieceRegisters[index] = _pieceRegisters[^1];
            _pieceRegisters.RemoveAt(_pieceRegisters.Count - 1);
            if (download.Size == PieceSize(download.PieceIndex)) 
            {
                _requestedPieces[download.PieceIndex] = true;
            }
        }
        return new(download.PieceIndex, downloadOffset, downloadSize);
    }

    public PieceRequest? AssignBlockRequest(BitArray ownedPieces, int downloadedPiecesOffset)
    {
        var slot = SeachPiece(ownedPieces, downloadedPiecesOffset);
        if (slot is not null)
        {
            return AllocateDownload(slot.Value.Download, slot.Value.Index);
        }
        return null;
    }

    private (int Index, PieceDownload Download)? SeachPiece(BitArray ownedPieces, int downloadedPiecesOffset)
    {
        foreach (var (index, pieceDownload) in _pieceRegisters.Select((v, i) => (i, v)))
        {
            if (pieceDownload.Size > pieceDownload.Downloading && ownedPieces[pieceDownload.PieceIndex])
            {
                return (index, pieceDownload);
            }
        }
        var creation = CreateDownload(ownedPieces, downloadedPiecesOffset);
        if (creation is null) return default;
        _pieceRegisters.Add(creation);
        return (_pieceRegisters.Count - 1, creation);
    }

    private PieceDownload? CreateDownload(BitArray ownedPieces, int downloadedPiecesOffset)
    {
        if (_rarestPieces.Count != 0)
        {
            var rarePiece = FindNextPiece(_rarestPieces, ownedPieces);
            if (rarePiece is not null)
            {
                _rarestPieces.Remove(rarePiece.Value.index);
                return rarePiece.Value.download;
            }
        }
        var nextPiece = FindNextPiece(Enumerable.Range(downloadedPiecesOffset, Torrent.NumberOfPieces - downloadedPiecesOffset), ownedPieces);
        if (nextPiece is not null)
        {
            return nextPiece.Value.download;
        }
        return default;
    }

    private bool CanBeRequested(int pieceIndex, BitArray ownedPieces)
    {
        return !_requestedPieces[pieceIndex] && ownedPieces[pieceIndex];
    }

    private (int index, PieceDownload download)? FindNextPiece(IEnumerable<int> downloads, BitArray ownedPieces)
    {
        return downloads
            .Where(piece => CanBeRequested(piece, ownedPieces))
            .Select((piece, index) => (index, new PieceDownload(PieceSize(piece), piece, new(Config.RequestSize))))
            .First();
    }

    private int PieceSize(int piece)
    {
        return (int)long.Min(Torrent.PieceSize, Torrent.TotalSize - piece * Torrent.PieceSize);
    }
}
