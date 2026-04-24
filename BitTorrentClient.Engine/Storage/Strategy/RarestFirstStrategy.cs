using BitTorrentClient.Engine.Storage.Interface;
using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Engine.Storage.Strategy;

class RarestFirstStrategy : IPieceSelectionStrategy
{
    public int SelectPieces(ZeroCopyBitArray requestedPieces, IEnumerable<LazyBitArray> peerPieces, Span<int> buffer)
    {
        var stack = new PriorityStack<(int, int)>(buffer.Length, Comparer<(int, int)>.Create((a, b) => b.Item2 - a.Item2));
        for (int i = 0; i < requestedPieces.Length; i++)
        {
            if (requestedPieces[i]) continue;
            int count = 0;
            foreach (LazyBitArray pieces in peerPieces)
            {
                if (pieces[i]) count++;
            } 
            stack.Include((i, count));
        }
        int len = 0;
        foreach (var (index, (piece, _)) in stack.Index())
        {
            buffer[index] = piece;
            len++;
        }
        return len;
    }

}