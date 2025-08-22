namespace BitTorrentClient.Utils;

public readonly struct ZeroCopyBitArray
{
    private readonly byte[] _buffer;
    private readonly int _bitLength;

    public ZeroCopyBitArray(int length)
    {
        _bitLength = length;
        int realLength = length / 8;
        if (length % 8 != 0) realLength++;
        _buffer = new byte[realLength];
    }

    public byte[] Buffer => _buffer;
    public int Length => _bitLength;

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
                target |= mask;
            }
            else
            {
                target &= unchecked((byte)~mask);
            }
        }
    }
}
