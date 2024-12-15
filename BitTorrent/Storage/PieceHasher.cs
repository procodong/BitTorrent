using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Storage;
public class PieceHasher(int blockCount)
{
    private int _offset = 0;
    private readonly byte[]?[] _blocks = new byte[]?[blockCount];
    private readonly SHA1 _hasher = SHA1.Create();

    public void Hash(byte[] data, int index)
    {
        _blocks[index] = data;
        for (; _offset < _blocks.Length; _offset++)
        {
            ref var block = ref _blocks[_offset];
            if (block is null) break;
            _hasher.TransformBlock(block, 0, block.Length, null, 0);
            ArrayPool<byte>.Shared.Return(block);
            block = null;
        }
    }

    public byte[] Finish()
    {
        _hasher.TransformFinalBlock([], 0, 0);
        return _hasher.Hash!;
    }
}
