using BencodeNET.Torrents;
using BitTorrentClient.Application.Infrastructure.Downloads;
using BitTorrentClient.Application.Infrastructure.Peers.Exceptions;
using BitTorrentClient.Application.Infrastructure.Storage.Distribution;
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
    private readonly DownloadStorage _storage;
    private readonly Torrent _torrent;

    public BlockStorage(ChannelWriter<int> haveWriter, Torrent torrent, DownloadStorage storage)
    {
        _haveWriter = haveWriter;
        _storage = storage;
        _torrent = torrent;
    }

    public async Task SaveBlockAsync(Stream stream, Block block, CancellationToken cancellationToken = default)
    {
        await block.Piece.Hasher.SaveBlock(stream, block.Begin, cancellationToken);
        int newDownloaded = Interlocked.Add(ref block.Piece.Downloaded, block.Length);
        foreach (var (offset, array) in block.Piece.Hasher.HashReadyBlocks())
        {
            var file = _storage.GetStream(block.Piece.Index, offset, array.ExpectedSize);
            await file.WriteAsync(array.Buffer.AsMemory(), cancellationToken);
        }
        if (newDownloaded < block.Piece.Size)
        {
            return;
        }
        var correctHash = _torrent.Pieces.AsMemory(block.Piece.Index * SHA1.HashSizeInBytes, SHA1.HashSizeInBytes);
        if (!block.Piece.Hasher.Finish().AsSpan().SequenceEqual(correctHash.Span))
        {
            throw new InvalidDataException();
        }
        await _haveWriter.WriteAsync(block.Piece.Index, default);
    }

    public BlockStream RequestBlock(BlockRequest request)
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
        return (int)long.Min(_torrent.PieceSize, _torrent.TotalSize - piece * _torrent.PieceSize);
    }
}
