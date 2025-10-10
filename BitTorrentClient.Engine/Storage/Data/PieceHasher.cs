using System.Security.Cryptography;
using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Engine.Storage.Data;
public sealed class PieceHasher
{
    private readonly HashUnit[] _buffers;
    private readonly int _bufferSize;
    private readonly SHA1 _hasher;
    private int _hashedOffset;

    public PieceHasher(int bufferSize, int bufferCount)
    {
        _buffers = new HashUnit[bufferCount];
        _bufferSize = bufferSize;
        _hasher = SHA1.Create();
    }

    
    public Task SaveBlockAsync(Stream stream, int offset, CancellationToken cancellationToken = default)
    {
        var index = offset / _bufferSize;
        var bufferOffset = offset % _bufferSize;
        var hashUnit = _buffers[index];
        if (!hashUnit.Initialized)
        {
            hashUnit = new(new(_bufferSize));
            _buffers[index] = hashUnit;
        }
        return stream
            .ReadExactlyAsync(hashUnit.Array.Buffer.AsMemory(bufferOffset, (int)stream.Length), cancellationToken)
            .AsTask().ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully) Interlocked.Add(ref _buffers[index].Written, (int)stream.Length);
            }, cancellationToken);
    }

    public IEnumerable<(int Offset, MaybeRentedArray<byte> Buffer)> HashReadyBlocks()
    {
        for (; _hashedOffset < _buffers.Length; _hashedOffset++)
        {
            var hashUnit = _buffers[_hashedOffset];
            if (hashUnit.Written != hashUnit.Array.Size) break;
            _hasher.TransformBlock(hashUnit.Array.Buffer, 0, hashUnit.Array.Size, null, 0);
            _buffers[_hashedOffset] = default;
            var pieceOffset = _hashedOffset * _bufferSize;
            _hashedOffset++;
            yield return (pieceOffset, hashUnit.Array);
        }
    }

    public byte[] Finish()
    {
        _hasher.TransformFinalBlock([], 0, 0);
        return _hasher.Hash!;
    }
}

struct HashUnit
{
    public volatile int Written;
    public bool Initialized { get; }
    public MaybeRentedArray<byte> Array { get; }

    public HashUnit(MaybeRentedArray<byte> array)
    {
        Array = array;
        Initialized = true;
    }
}

