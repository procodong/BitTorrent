using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Engine.Storage.Distribution;

class PiecesCursor
{
    private readonly int[] _buffer;
    public int Position {get; private set;}
    public int Length {get; private set;}

    public PiecesCursor(int[] buffer)
    {
        _buffer = buffer;
    }
    
    public bool TryGetNext(LazyBitArray blacklist, out int found)
    {
        for (int i = 0; i < Length; i++)
        {
            var piece = _buffer[i];
            if (blacklist[piece]) continue;
            found = piece;
            
            return true;
        }
        found = default;
        return false;
    }

    public Span<int> StartUpdate()
    {
        var length = Length - Position;
        _buffer.AsSpan(Position, length).CopyTo(_buffer);
        var buffer = _buffer.AsSpan(0, Length);
        Length = length;
        Position = 0;
        return buffer;
    }

    public void EndUpdate(int count)
    {
        Length += count;
    }
}