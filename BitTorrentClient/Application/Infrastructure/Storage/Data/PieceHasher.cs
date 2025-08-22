using BitTorrentClient.Helpers.DataStructures;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.Infrastructure.Storage.Data;
public class PieceHasher
{
    private readonly HashUnit[] _buffers;
    private readonly int _bufferSize;
    private readonly int _blockSize;
    private readonly SHA1 _hasher;
    private int _offset;

    public PieceHasher(int blockSize, int bufferBlockCount, int totalBlockCount)
    {
        _buffers = new HashUnit[totalBlockCount / bufferBlockCount * blockSize];
        _bufferSize = bufferBlockCount * blockSize;
        _blockSize = blockSize;
        _hasher = SHA1.Create();
    }

    public async Task SaveBlock(Stream stream, int offset, CancellationToken cancellationToken = default)
    {
        int index = offset / _bufferSize - _offset;
        int bufferOffset = offset % _bufferSize;
        HashUnit hashUnit = _buffers[_offset];
        if (hashUnit.Array.Buffer is null) hashUnit = _buffers[index] with { Array = new MaybeRentedArray<byte>(_bufferSize) };
        await stream.ReadExactlyAsync(hashUnit.Array.Buffer.AsMemory(bufferOffset, (int)stream.Length), cancellationToken);
        Interlocked.Add(ref _buffers[index].Written, (int)stream.Length);
    }

    public IEnumerable<(int Offset, MaybeRentedArray<byte>)> HashReadyBlocks()
    {
        for (; _offset < _buffers.Length; _offset++)
        {
            HashUnit hashUnit = _buffers[_offset];
            if (hashUnit.Written != hashUnit.Array.Size) break;
            _hasher.TransformBlock(hashUnit.Array.Buffer, 0, hashUnit.Array.Size, null, 0);
            _buffers[_offset] = default;
            int pieceOffset = _offset * _blockSize;
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

record struct HashUnit(MaybeRentedArray<byte> Array)
{
    public volatile int Written;
}

