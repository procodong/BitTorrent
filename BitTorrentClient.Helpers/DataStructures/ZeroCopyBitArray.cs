namespace BitTorrentClient.Helpers.DataStructures;

public readonly struct ZeroCopyBitArray
{
    private readonly byte[] _buffer;
    private readonly int _bitLength;

    public ZeroCopyBitArray(int length)
    {
        _bitLength = length;
        var realLength = length / 8;
        if (length % 8 != 0) realLength++;
        _buffer = new byte[realLength];
    }

    public ZeroCopyBitArray(byte[] buffer)
    {
        _buffer = buffer;
        _bitLength = buffer.Length * 8;
    }

    public byte[] Buffer => _buffer;
    public int Length => _bitLength;

    public bool this[int index]
    {
        get
        {
            var value = _buffer[index / 8];
            var offset = index % 8;
            var mask = (byte)(1 << offset);
            return (value & mask) != 0;
        }
        set
        {
            ref var target = ref _buffer[index / 8];
            var offset = index % 8;
            var mask = (byte)(1 << offset);
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
