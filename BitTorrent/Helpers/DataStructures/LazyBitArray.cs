namespace BitTorrentClient.Helpers.DataStructures;
public class LazyBitArray
{
    private ZeroCopyBitArray _bitfield;
    private BitfieldState _state;
    private readonly int _size;

    public bool AllSet => _state == BitfieldState.AllSet;
    public bool NoneSet => _state == BitfieldState.NoneSet;
    public byte[] Buffer => _bitfield.Buffer;

    public LazyBitArray(ZeroCopyBitArray bitfield)
    {
        _bitfield = bitfield;
        _state = BitfieldState.Unknown;
        _size = bitfield.Length;
    }

    public LazyBitArray(int size, bool allSet = false)
    {
        _state = allSet ? BitfieldState.AllSet : BitfieldState.NoneSet;
        _size = size;
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
                    _bitfield = new(_size);
                    for (int i = 0; i < _bitfield.Buffer.Length; i++)
                    {
                        _bitfield.Buffer[i] = byte.MaxValue;
                    }
                    _state = BitfieldState.Unknown;
                    break;
                case BitfieldState.NoneSet:
                    if (!value) return;
                    _bitfield = new(_size);
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