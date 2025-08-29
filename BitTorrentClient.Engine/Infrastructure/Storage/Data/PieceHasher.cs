using System.Security.Cryptography;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Extensions;

namespace BitTorrentClient.Engine.Infrastructure.Storage.Data;
public class PieceHasher
{
    private readonly HashUnit[] _buffers;
    private readonly int _bufferSize;
    private readonly SHA1 _hasher;
    private int _offset;

    public PieceHasher(int bufferSize, int bufferCount)
    {
        _buffers = new HashUnit[bufferCount];
        _bufferSize = bufferSize;
        _hasher = SHA1.Create();
    }
    
    public async Task SaveBlockAsync(Stream stream, int offset, CancellationToken cancellationToken = default)
    {
        var index = offset / _bufferSize;
        var bufferOffset = offset % _bufferSize;
        var hashUnit = _buffers[index];
        if (!hashUnit.Initialized)
        {
            hashUnit = new(new(_bufferSize));
            _buffers[index] = hashUnit;
        }
        await stream.ReadExactlyAsync(hashUnit.Array.Buffer.AsMemory(bufferOffset, (int)stream.Length), cancellationToken);
        Interlocked.Add(ref _buffers[index].Written, (int)stream.Length);
    }

    public IEnumerable<(int Offset, MaybeRentedArray<byte>)> HashReadyBlocks()
    {
        for (; _offset < _buffers.Length; _offset++)
        {
            var hashUnit = _buffers[_offset];
            if (hashUnit.Written != hashUnit.Array.Size) break;
            _hasher.TransformBlock(hashUnit.Array.Buffer, 0, hashUnit.Array.Size, null, 0);
            _buffers[_offset] = default;
            var pieceOffset = _offset * _bufferSize;
            _offset++;
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

