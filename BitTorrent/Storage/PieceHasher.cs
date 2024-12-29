using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Storage;
public class PieceHasher
{
    private readonly (byte[], int Length)?[] _blocks;
    private readonly SHA1 _hasher;
    private int _offset;

    public PieceHasher(int blockCount)
    {
        _blocks = new (byte[], int)?[blockCount];
        _hasher = SHA1.Create();
    }

    public void Hash(byte[] data, int length, int index)
    {
        _blocks[index] = (data, length);
        for (; _offset < _blocks.Length; _offset++)
        {
            ref var block = ref _blocks[_offset];
            if (block is null) break;
            var (buf, len) = block.Value;
            _hasher.TransformBlock(buf, 0, len, null, 0);
            ArrayPool<byte>.Shared.Return(buf);
            block = null;
        }
    }

    public byte[] Finish()
    {
        _hasher.TransformFinalBlock([], 0, 0);
        return _hasher.Hash!;
    }
}
