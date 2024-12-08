using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.PieceSaver;
public class PieceHasher(int blockSize)
{
    private int _offset = 0;
    private readonly List<byte[]?> _blocks = [];
    private readonly int _blockSize = blockSize;
    private readonly SHA1 _hasher = SHA1.Create();

    public void Hash(byte[] data, int offset)
    {
        int index = offset / _blockSize - _offset;
        while (index >= _blocks.Count)
        {
            _blocks.Add(null);
        }
        int removeCount = 0;
        _blocks[index] = data;
        foreach (var block in _blocks.TakeWhile(v => v is not null))
        {
            _hasher.TransformBlock(block!, 0, block!.Length, null, 0);
            removeCount++;
        }
        _offset += removeCount;
        for (int i = 0; i < removeCount; i++)
        {
            ArrayPool<byte>.Shared.Return(_blocks[i]!);
        }
        _blocks.RemoveRange(0, removeCount);
    }

    public byte[] Finish()
    {
        _hasher.TransformFinalBlock([], 0, 0);
        return _hasher.Hash!;
    }
}
