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
        if (hashUnit.Buffer.Array is null) hashUnit = _buffers[index] with { Buffer = new ArraySegment<byte>(ArrayPool<byte>.Shared.Rent(_bufferSize), 0, _bufferSize) };
        await stream.ReadExactlyAsync(hashUnit.Buffer.Array.AsMemory(bufferOffset, (int)stream.Length), cancellationToken);
        Interlocked.Add(ref _buffers[index].Written, (int)stream.Length);
    }

    public IEnumerable<(int Offset, ArraySegment<byte>)> HashReadyBlocks()
    {
        for (; _offset < _buffers.Length; _offset++)
        {
            HashUnit hashUnit = _buffers[_offset];
            if (hashUnit.Written != hashUnit.Buffer.Count) break;
            _hasher.TransformBlock(hashUnit.Buffer.Array!, hashUnit.Buffer.Offset, hashUnit.Buffer.Count, null, 0);
            _buffers[_offset] = default;
            int pieceOffset = _offset * _blockSize;
            _offset++;
            yield return (pieceOffset, hashUnit.Buffer);
        }
    }

    public byte[] Finish()
    {
        _hasher.TransformFinalBlock([], 0, 0);
        return _hasher.Hash!;
    }
}

record struct HashUnit(ArraySegment<byte> Buffer)
{
    public volatile int Written;
}

