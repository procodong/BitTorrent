
using BencodeNET.Torrents;
using BitTorrent.Errors;
using BitTorrent.Files;
using BitTorrent.Models.Application;
using BitTorrent.Models.Messages;
using BitTorrent.Models.Peers;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Downloads;
public class Download(Torrent torrent, FileManager files, Config config)
{
    public readonly Torrent Torrent = torrent;
    public readonly Config Config = config;
    public readonly BitArray DownloadedPieces = new(new int[torrent.NumberOfPieces / 32]);
    private readonly PeerStatistics _statistics = new();
    private readonly FileManager _files = files;
    private readonly PieceDownloadSlot[] _pieceDownloads = new PieceDownloadSlot[config.ConcurrentPieceDownloads];
    private int _downloadingPiecesOffset = 0;
    private bool _pieceDownloadsInit = false;
    private readonly ConcurrentQueue<int> _rarestPieces = new();


    public Stream RequestBlock(PieceRequest request)
    {
        return _files.Read(request.Length, request.Index, request.Begin);
    }

    public async Task SavePieceAsync(Stream piece, int pieceIndex)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(1 << 14);
        var destinationStream = _files.Read(pieceIndex);
        int readBytes;
        using SHA1 hasher = SHA1.Create();
        while ((readBytes = await piece.ReadAsync(buffer)) != 0)
        {
            await destinationStream.WriteAsync(buffer.AsMemory(..readBytes));
            hasher.TransformBlock(buffer, 0, readBytes, null, 0);
        }
        ArrayPool<byte>.Shared.Return(buffer);
        hasher.TransformFinalBlock([], 0, 0);
        Memory<byte> correctHash = Torrent.Pieces.AsMemory(pieceIndex * 20, 20);
        IEnumerable<byte> correctHashIter = MemoryMarshal.ToEnumerable<byte>(correctHash);
        if (!hasher.Hash!.SequenceEqual(correctHashIter))
        {
            throw new BadPeerException();
        }
        _statistics.IncrementDownloaded(piece.Length);
    }

    public async Task SaveBlockAsync(Stream stream, int download, int offset)
    {
        int length = (int)(stream.Length - stream.Position);
        PieceDownloadSlot pieceDownload = _pieceDownloads[download];
        await pieceDownload.FileLock.WaitAsync();
        pieceDownload.FileBuffer.Position = offset;
        await stream.CopyToAsync(pieceDownload.FileBuffer);
        pieceDownload.FileLock.Release();
        pieceDownload.Download!.Downloaded += length;
    }

    public async Task<PieceRequest> AssignPieceAsync(BitArray ownedPieces, int downloadedPiecesOffset, int previousDownload)
    {
        PieceDownloadSlot current = _pieceDownloads[previousDownload];

        int oldDownloading = current.Downloading;
        if (oldDownloading >= current.Size)
        {

        }
        int downloadSize = int.Min(current.Size - oldDownloading, Config.RequestSize);
        int newDownloading = Interlocked.Add(ref current.Downloading, downloadSize);
        int downloadOffset = newDownloading - downloadSize;
        if (downloadOffset < current.Size)
        {
            return new(current.PieceIndex, downloadOffset, downloadSize);
        }
    }

    public IEnumerable<PieceRequest> AssignInitialBlockRequests()
    {
        if (!_pieceDownloadsInit)
        {

        }
    }

    private void InitializePieceDownloadBuffers()
    {
        for (int i = 0; i < _pieceDownloads.Length; i++)
        {

        }
    }

    private PieceDownload? SeachPiece(BitArray ownedPieces, int downloadedPiecesOffset)
    {
        foreach (var download in _pieceDownloads)
        {
            if (download.Size > download.Downloading && ownedPieces[download.PieceIndex])
            {
                return download;
            }
        }
        return null;
    }
}
