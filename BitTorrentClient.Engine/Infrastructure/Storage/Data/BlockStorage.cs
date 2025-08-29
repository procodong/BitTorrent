using System.Security.Cryptography;
using System.Threading.Channels;
using BitTorrentClient.Engine.Infrastructure.Peers.Exceptions;
using BitTorrentClient.Engine.Infrastructure.Storage.Distribution;
using BitTorrentClient.Engine.Models.Downloads;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;
using BitTorrentClient.Protocol.Presentation.Torrent;

namespace BitTorrentClient.Engine.Infrastructure.Storage.Data;
public class BlockStorage
{
    private readonly ChannelWriter<int> _haveWriter;
    private readonly DataStorage _storage;
    private readonly DownloadData _downloadData;

    public BlockStorage(DownloadData downloadData, DataStorage storage, ChannelWriter<int> haveWriter)
    {
        _haveWriter = haveWriter;
        _storage = storage;
        _downloadData = downloadData;
    }

    public async Task SaveBlockAsync(Stream stream, Block block, CancellationToken cancellationToken = default)
    {
        Task readStream;
        lock (block.Piece.Hasher) readStream = block.Piece.Hasher.SaveBlockAsync(stream, block.Begin, cancellationToken);
        await readStream;
        lock (block.Piece.Hasher)
        {
            foreach (var (offset, buffer) in block.Piece.Hasher.HashReadyBlocks())
            {
                _ = _storage.WriteDataAsync(block.Piece.Index * _downloadData.PieceSize + offset, buffer);
            }
        }
        var newDownloaded = Interlocked.Add(ref block.Piece.Downloaded, block.Length);
        if (newDownloaded < block.Piece.Size)
        {
            return;
        }
        if (!block.Piece.Hasher.Finish().AsSpan().SequenceEqual(_downloadData.PieceHashes.Span.Slice(block.Piece.Index * SHA1.HashSizeInBytes, SHA1.HashSizeInBytes)))
        {
            throw new InvalidDataException();
        }
        await _haveWriter.WriteAsync(block.Piece.Index, CancellationToken.None); // SEND AFTER DATA IS WRITTEN TO STORAGE
    }

    public BlockStream RequestBlock(BlockRequest request)
    {
        ValidateRequest(request);
        return _storage.GetData(request.Index * _downloadData.PieceSize + request.Begin, request.Length);
    }

    private void ValidateRequest(BlockRequest request)
    {
        var size = PieceSize(request.Index);
        var end = request.Begin + request.Length;
        if (end > size || end < 0)
        {
            throw new BadPeerException(PeerErrorReason.InvalidRequest);
        }
    }

    private int PieceSize(int piece)
    {
        return (int)long.Min(_downloadData.PieceSize, _downloadData.Size - piece * _downloadData.PieceSize);
    }
}
