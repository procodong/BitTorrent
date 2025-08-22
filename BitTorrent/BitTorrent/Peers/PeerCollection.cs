using System.Collections;
using System.Threading.Channels;
using BitTorrentClient.BitTorrent.Peers.Connections;
using BitTorrentClient.Helpers;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Models.Peers;

namespace BitTorrentClient.BitTorrent.Peers;
public class PeerCollection : IEnumerable<PeerConnector>
{
    private readonly SlotMap<PeerConnector> _peers = [];
    private readonly HashSet<ReadOnlyMemory<byte>> _peerIds = new(new MemoryComparer<byte>());
    private readonly PeerSpawner _spawner;
    private readonly int _pieceCount;
    private List<PeerAddress> _potentialPeers = [];
    private IEnumerator<(int Index, PeerAddress Address)> _peerCursor;
    private int _missedPeers;

    public int Count => _peers.Count;

    public PeerCollection(PeerSpawner spawner, int pieceCount, int maxParallelPeers)
    {
        _spawner = spawner;
        _pieceCount = pieceCount;
        _missedPeers = maxParallelPeers;
        _peerCursor = Enumerable.Empty<(int, PeerAddress)>().GetEnumerator();
    }

    public void Add(PeerHandshaker stream)
    {
        if (stream.ReceivedHandShake is null)
        {
            throw new InvalidDataException("peer has to have received the hand shake");
        }

        byte[] peerId = stream.ReceivedHandShake.Value.PeerId;
        if (!_peerIds.Add(peerId))
        {
            ConnectNext();
            return;
        }
        var eventChannel = Channel.CreateBounded<PeerRelation>(16);
        var state = new PeerState(new(_pieceCount));
        var peerConnector = new PeerConnector(state, eventChannel.Writer, new());
        int index = _peers.Add(peerConnector);
        _ = _spawner.SpawnListener(stream, index, state, eventChannel.Reader, peerConnector.Canceller.Token).ConfigureAwait(false);
    }

    public async Task RemoveAsync(int? index)
    {
        if (index is not null)
        {
            await _peers[index.Value].Canceller.CancelAsync();
            _peers.Remove(index.Value);
        }
        ConnectNext();
    }

    private void ConnectNext()
    {
        if (_peerCursor.MoveNext())
        {
            _ = _spawner.SpawnConnect(_peerCursor.Current.Address).ConfigureAwait(false);
        }
        else
        {
            _missedPeers++;
        }
    }

    public void Update(List<PeerAddress> addresses)
    {
        var oldPeers = _potentialPeers.Take(_peerCursor.Current.Index).ToHashSet();
        addresses.RemoveAll(oldPeers.Contains);
        _potentialPeers = addresses;
        _peerCursor.Dispose();
        _peerCursor = addresses.Indexed().GetEnumerator();
        int i;
        for (i = 0; i < _missedPeers && _peerCursor.MoveNext(); i++)
        {
            _ = _spawner.SpawnConnect(_peerCursor.Current.Address);
        }
        _missedPeers -= i;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _peers.GetEnumerator();
    }

    public IEnumerator<PeerConnector> GetEnumerator()
    {
        return _peers.GetEnumerator();
    }
}
