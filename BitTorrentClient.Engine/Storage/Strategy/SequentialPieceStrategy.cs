using BitTorrentClient.Engine.Storage.Interface;
using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Engine.Storage.Strategy;

class SequentialPieceStrategy : IPieceSelectionStrategy
{
    private int _offset;

    public int SelectPieces(ZeroCopyBitArray requestedPieces, IEnumerable<LazyBitArray> peerPieces, Span<int> buffer)
    {
        while (_offset < requestedPieces.Length && requestedPieces[_offset]) _offset++;
        int index = 0;
        for (int i = _offset; i < requestedPieces.Length; i++)
        {
            if (requestedPieces[i]) continue;
            buffer[index] = i;
            index++;
        }
        return index;
    }
}