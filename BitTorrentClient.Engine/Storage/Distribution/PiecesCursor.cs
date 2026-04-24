using System.Collections;
using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Engine.Storage.Distribution;

public class PiecesCursor
{
    private readonly int[] _buffer;
    private readonly BitArray _usedPieces;
    private int _position;
    private int _length;

    public PiecesCursor(int bufferSize)
    {
        _buffer = new int[bufferSize];
        _usedPieces = new BitArray(bufferSize);
    }
    
    public bool TryGetNext(LazyBitArray ownedPieces, out int found)
    {
        for (int i = _position; i < _length; i++)
        {
            var piece = _buffer[i];
            if (!ownedPieces[piece] || _usedPieces[i]) continue;
            found = piece;
            _usedPieces[i] = true;
            while (_usedPieces[i])
            {
                _position++;
            }
            return true;
        }
        found = default;
        return false;
    }

    public void SupplyPieces(Func<Span<int>, int> action)
    {
        _length = action(_buffer);
        _position = 0;
        _usedPieces.SetAll(false);
    }
}