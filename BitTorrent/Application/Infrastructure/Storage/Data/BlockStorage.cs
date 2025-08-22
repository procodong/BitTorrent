using BencodeNET.Torrents;
using BitTorrentClient.Application.Infrastructure.Downloads;
using BitTorrentClient.Application.Infrastructure.Peers.Exceptions;
using BitTorrentClient.Application.Infrastructure.Storage.Distribution;
using BitTorrentClient.Models.Messages;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.Infrastructure.Storage.Data;
public class BlockStorage
{
    private readonly ChannelWriter<int> _haveWriter;
    private readonly DownloadStorage _storage;
    private readonly DownloadState _state;

    public BlockStorage(ChannelWriter<int> haveWriter, DownloadState state, DownloadStorage storage)
    {
        _haveWriter = haveWriter;
        _storage = storage;
        _state = state;
    }

    public async Task SaveBlockAsync(Stream stream, Block block, CancellationToken cancellationToken = default)
    {
        await stream.ReadExactlyAsync(block.Piece.Buffer.AsMemory(block.Begin, block.Length), cancellationToken);
        int newDownloaded = Interlocked.Add(ref block.Piece.Downloaded, block.Length);
        _state.RecentDataTransfer.AtomicAddDownload(block.Length);
        if (newDownloaded < block.Piece.Size)
        {
            return;
        }
        var correctHash = _state.Download.Torrent.Pieces.AsMemory(block.Piece.PieceIndex * SHA1.HashSizeInBytes, SHA1.HashSizeInBytes);
        var hash = SHA1.HashData(block.Piece.Buffer);
        if (!hash.AsSpan().SequenceEqual(correctHash.Span))
        {
            throw new InvalidDataException();
        }
        var files = _storage.GetStream(block.Piece.PieceIndex, block.Begin, block.Length);
        await files.WriteAsync(block.Piece.Buffer.AsMemory(..block.Piece.Size), cancellationToken);
        ArrayPool<byte>.Shared.Return(block.Piece.Buffer);
        await _haveWriter.WriteAsync(block.Piece.PieceIndex, default);
    }

    public PieceStream RequestBlock(BlockRequest request)
    {
        ValidateRequest(request);
        return _storage.GetStream(request.Index, request.Begin, request.Length);
    }

    private void ValidateRequest(BlockRequest request)
    {
        int size = PieceSize(request.Index);
        int end = request.Begin + request.Length;
        if (end > size || end < 0)
        {
            throw new BadPeerException(PeerErrorReason.InvalidRequest);
        }
    }

    private int PieceSize(int piece)
    {
        return (int)long.Min(_state.Download.Torrent.PieceSize, _state.Download.Torrent.TotalSize - piece * _state.Download.Torrent.PieceSize);
    }
}
