using BencodeNET.Torrents;
using BitTorrentClient.Application.Infrastructure.Peers.Exceptions;
using BitTorrentClient.Application.Infrastructure.Storage.Distribution;
using BitTorrentClient.Models.Application;
using System.Security.Cryptography;
using System.Threading.Channels;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Application.Infrastructure.Storage.Data;
internal class BlockStorage
{
    private readonly ChannelWriter<int> _haveWriter;
    private readonly ChannelWriter<DownloadExecutionState> _downloadStateWriter;
    private readonly DataStorage _storage;
    private readonly DownloadData _downloadData;

    public BlockStorage(DownloadData downloadData, DataStorage storage, ChannelWriter<int> haveWriter, ChannelWriter<DownloadExecutionState> downloadStateWriter)
    {
        _downloadStateWriter = downloadStateWriter;
        _haveWriter = haveWriter;
        _storage = storage;
        _downloadData = downloadData;
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
                _ = _storage.WriteDataAsync(block.Piece.Index * _downloadData.PieceSize + offset, buffer);
            }
        }
        int newDownloaded = Interlocked.Add(ref block.Piece.Downloaded, block.Length);
        if (newDownloaded < block.Piece.Size)
        {
            return;
        }
        if (!block.Piece.Hasher.Finish().AsSpan().SequenceEqual(_downloadData.PieceHashes.Span.Slice(block.Piece.Index * SHA1.HashSizeInBytes, SHA1.HashSizeInBytes)))
        {
            throw new InvalidDataException();
        }
        await _haveWriter.WriteAsync(block.Piece.Index, CancellationToken.None);
    }

    public BlockStream RequestBlock(BlockRequest request)
    {
        ValidateRequest(request);
        return _storage.GetData(request.Index * _downloadData.PieceSize + request.Begin, request.Length);
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
        return (int)long.Min(_downloadData.PieceSize, _downloadData.Size - piece * _downloadData.PieceSize);
    }
}
