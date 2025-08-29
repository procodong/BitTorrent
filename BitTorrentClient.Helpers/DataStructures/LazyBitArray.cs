namespace BitTorrentClient.Helpers.DataStructures;
public class LazyBitArray
{
    private ZeroCopyBitArray _bitfield;
    private BitfieldState _state;
    private readonly int _bitSize;

    public bool AllSet => _state == BitfieldState.AllSet;
    public bool NoneSet => _state == BitfieldState.NoneSet;
    public int BitSize => _bitSize;
    public byte[] Buffer => _bitfield.Buffer;

    public LazyBitArray(ZeroCopyBitArray bitfield)
    {
        _bitfield = bitfield;
        _state = BitfieldState.Unknown;
        _bitSize = bitfield.Length;
    }

    public LazyBitArray(int bitSize, bool allSet = false)
    {
        _state = allSet ? BitfieldState.AllSet : BitfieldState.NoneSet;
        _bitSize = bitSize;
        
    }

    public bool this[int index]
    {
        get => _state switch
        {
            BitfieldState.AllSet => true,
            BitfieldState.NoneSet => false,
            BitfieldState.Unknown => _bitfield[index],
            _ => throw new InvalidDataException()
        };
        set
        {
            switch (_state)
            {
                case BitfieldState.AllSet:
                    if (value) return;
                    _bitfield = new(_bitSize);
                    _bitfield.Buffer.AsSpan().Fill(byte.MaxValue);
                    _state = BitfieldState.Unknown;
                    break;
                case BitfieldState.NoneSet:
                    if (!value) return;
                    _bitfield = new(_bitSize);
                    _state = BitfieldState.Unknown;
                    break;
            }
            _bitfield[index] = value;
        }
    }
}

enum BitfieldState
{
    AllSet,
    NoneSet,
    Unknown
}