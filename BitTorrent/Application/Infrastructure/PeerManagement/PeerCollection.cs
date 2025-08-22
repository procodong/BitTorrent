using System.Collections;
using System.Threading.Channels;
using BitTorrentClient.Application.EventHandling.PeerManagement;
using BitTorrentClient.BitTorrent.Peers.Connections;
using BitTorrentClient.Helpers;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Protocol.Networking.PeerWire;

namespace BitTorrentClient.Application.Infrastructure.PeerManagement;
public class PeerCollection : IPeerCollection, IEnumerable<PeerHandle>
{
    private readonly SlotMap<PeerHandle> _peers = [];
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

    public void Add(RespondedPeerHandshaker stream)
    {
        byte[] peerId = stream.ReceivedHandshake.PeerId;
        if (!_peerIds.Add(peerId))
        {
            ConnectNext();
            return;
        }
        var eventChannel = Channel.CreateBounded<PeerRelation>(16);
        var state = new PeerState(new(_pieceCount));
        var peerConnector = new PeerHandle(state, eventChannel.Writer, new());
        int index = _peers.Add(peerConnector);
        _ = _spawner.SpawnListener(stream, index, state, eventChannel.Reader, peerConnector.Canceller.Token).ConfigureAwait(false);
    }

    public void Remove(int? index)
    {
        if (index is not null)
        {
            _peers[index.Value].Canceller.Cancel();
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

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _peers.GetEnumerator();
    }

    public IEnumerator<PeerHandle> GetEnumerator()
    {
        return _peers.GetEnumerator();
    }

    public void Feed(PeerAddress[] addresses)
    {
        var peers = addresses.ToList();
        var oldPeers = _potentialPeers.Take(_peerCursor.Current.Index).ToHashSet();
        peers.RemoveAll(oldPeers.Contains);
        _potentialPeers = peers;
        _peerCursor.Dispose();
        _peerCursor = peers.Indexed().GetEnumerator();
        int i;
        for (i = 0; i < _missedPeers && _peerCursor.MoveNext(); i++)
        {
            _ = _spawner.SpawnConnect(_peerCursor.Current.Address).ConfigureAwait(false);
        }
        _missedPeers -= i;
    }
}
