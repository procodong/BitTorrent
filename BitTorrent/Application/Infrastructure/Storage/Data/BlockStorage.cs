using BencodeNET.Torrents;
using BitTorrentClient.Application.Infrastructure.Downloads;
using BitTorrentClient.Application.Infrastructure.Peers.Exceptions;
using BitTorrentClient.Application.Infrastructure.Storage.Distribution;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Models.Messages;
using BitTorrentClient.Models.Peers;
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
    private readonly ChannelWriter<DownloadExecutionState> _downloadStateWriter;
    private readonly DataStorage _storage;
    private readonly Torrent _torrent;

    public BlockStorage(Torrent torrent, DataStorage storage, ChannelWriter<int> haveWriter, ChannelWriter<DownloadExecutionState> downloadStateWriter)
    {
        _downloadStateWriter = downloadStateWriter;
        _haveWriter = haveWriter;
        _storage = storage;
        _torrent = torrent;
    }

    public async Task SaveBlockAsync(Stream stream, Block block, CancellationToken cancellationToken = default)
    {
        Task readStream;
        lock (block.Piece.Hasher) readStream = block.Piece.Hasher.SaveBlock(stream, block.Begin, cancellationToken);
        await readStream;
        lock (block.Piece.Hasher)
        {
            foreach (var (offset, buffer) in block.Piece.Hasher.HashReadyBlocks())
            {
                _ = _storage.WriteDataAsync(block.Piece.Index * _torrent.PieceSize + offset, buffer, true);
            }
        }
        int newDownloaded = Interlocked.Add(ref block.Piece.Downloaded, block.Length);
        if (newDownloaded < block.Piece.Size)
        {
            return;
        }
        var correctHash = _torrent.Pieces.AsMemory(block.Piece.Index * SHA1.HashSizeInBytes, SHA1.HashSizeInBytes);
        if (!block.Piece.Hasher.Finish().AsSpan().SequenceEqual(correctHash.Span))
        {
            throw new InvalidDataException();
        }
        await _haveWriter.WriteAsync(block.Piece.Index, CancellationToken.None);
    }

    public BlockStream RequestBlock(BlockRequest request)
    {
        ValidateRequest(request);
        return _storage.GetData(request.Index * _torrent.PieceSize + request.Begin, request.Length);
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
        return (int)long.Min(_torrent.PieceSize, _torrent.TotalSize - piece * _torrent.PieceSize);
    }
}
