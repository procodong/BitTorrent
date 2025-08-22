namespace BitTorrent.Utils;

public class ZeroCopyBitArray
{
    private readonly byte[] _buffer;

    public ZeroCopyBitArray(int length)
    {
        _buffer = new byte[length];
    }

    public byte[] Buffer => _buffer;

    public bool this[int index]
    {
        get
        {
            byte value = _buffer[index / 8];
            int offset = index % 8; 
            byte mask = (byte)(1 << offset);
            return (value & mask) != 0;
        }
        set
        {
            ref byte target = ref _buffer[index / 8];
            int offset = index % 8; 
            byte mask = (byte)(1 << offset);
            if (value)
            {
                target &= mask;
            }
            else
            {
                target ^= mask;
            }
        }
    }
}
