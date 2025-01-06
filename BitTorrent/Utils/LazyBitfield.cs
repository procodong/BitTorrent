using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Utils;
public class LazyBitfield
{
    private BitArray _bitfield;
    private BitfieldState _state;
    private readonly int _size;

    public bool AllSet => _state == BitfieldState.AllSet;
    public bool NoneSet => _state == BitfieldState.NoneSet;

    public LazyBitfield(BitArray bitfield)
    {
        _bitfield = bitfield;
        _state = BitfieldState.Unknown;
        _size = bitfield.Length;
    }

    public LazyBitfield(int size, bool allSet = false)
    {
        _bitfield = null!;
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
                    _bitfield.SetAll(true);
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