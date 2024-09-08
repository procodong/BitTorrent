using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Utils;
public class BitArray(byte[] bytes)
{
    public byte[] Bytes = bytes;

    public bool this[int index]
    {
        get
        {
            var (realIndex, bitIndex) = GetIndex(index);
            return (Bytes[realIndex] >> bitIndex & 1) == 1;
        }
        set
        {
            var (realIndex, bitIndex) = GetIndex(index);
            byte mask = (byte)(1 << bitIndex);

            if (value)
            {
                Bytes[realIndex] |= mask;
            }
            else
            {
                Bytes[realIndex] &= (byte)~mask;
            }
        }
    }

    private static (int Index, int BitIndex) GetIndex(int index)
    {
        var realIndex = index / 8;
        var bitIndex = index % 8;
        return (realIndex, bitIndex);
    }
}
