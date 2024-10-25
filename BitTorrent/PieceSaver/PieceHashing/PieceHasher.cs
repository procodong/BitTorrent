using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.PieceSaver.PieceHashing;
public class PieceHasher(int blockSize)
{
    private int _offset = 0;
    private readonly List<byte[]?> _blocks = [];
    private readonly int _blockSize = blockSize;
    private readonly SHA1 _hasher = SHA1.Create();

    public void Hash(byte[] data, int offset)
    {
        int index = offset / _blockSize - _offset;
        if (index >= _blocks.Count)
        {
            int addCount = index + 1 - _blocks.Count;
            _blocks.AddRange(Enumerable.Range(0, addCount).Select<int, byte[]?>(_ => null));
        }
        int offsetAdd = 0;
        _blocks[index] = data;
        foreach (var block in _blocks.TakeWhile(v => v is not null))
        {
            _hasher.TransformBlock(block!, 0, block!.Length, null, 0);
            offsetAdd++;
        }
        _offset += offsetAdd;
        MoveBackBlocks(offsetAdd);
    }

    private void MoveBackBlocks(int offset)
    {
        foreach (var (i, block) in _blocks.Select((block, i) => (i, block)))
        {
            int newIndex = i - offset;
            if (newIndex < 0)
            {
                continue;
            }
            _blocks[newIndex] = block;
        }
    }
}
