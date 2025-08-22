using System.Collections;
using BitTorrentClient.Engine.Infrastructure.Peers.Interface;
using BitTorrentClient.Engine.Launchers.Interface;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Helpers.Utility;
using BitTorrentClient.Protocol.Transport.PeerWire.Connecting.Interface;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Engine.Infrastructure.Peers;
internal class PeerCollection : IPeerCollection, IEnumerable<PeerHandle>
{
    private readonly SlotMap<PeerHandle> _peers = [];
    private readonly HashSet<ReadOnlyMemory<byte>> _peerIds;
    private readonly PeerConnector _connector;
    private readonly IPeerLauncher _launcher;
    private IEnumerable<IPeerConnector> _potentialPeers = [];
    private IEnumerator<(int Index, IPeerConnector Address)> _peerCursor;
    private int _missedPeers;

    public int Count => _peers.Count;

    public PeerCollection(PeerConnector connector, IPeerLauncher launcher, int maxParallelPeers)
    {
        _connector = connector;
        _launcher = launcher;
        _missedPeers = maxParallelPeers;
        _peerCursor = DefaultValueEnumerator<(int, IPeerConnector)>.Instance;
        _peerIds = new(new MemoryComparer<byte>());
    }

    public void Add(PeerWireStream stream)
    {
        var peerId = stream.ReceivedHandshake.PeerId;
        if (!_peerIds.Add(peerId))
        {
            ConnectNext();
            return;
        }
        _peers.Add(i => _launcher.LaunchPeer(stream, i));
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
            _ = _connector.SpawnConnect(_peerCursor.Current.Address);
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

    public void Feed(IEnumerable<IPeerConnector> addresses)
    {
        var oldPeers = _potentialPeers.Take(_peerCursor.Current.Index).ToHashSet();
        _potentialPeers = addresses.Where(old => !oldPeers.Contains(old));
        _peerCursor.Dispose();
        _peerCursor = _potentialPeers.Indexed().GetEnumerator();
        int i;
        for (i = 0; i < _missedPeers && _peerCursor.MoveNext(); i++)
        {
            _ = _connector.SpawnConnect(_peerCursor.Current.Address);
        }
        _missedPeers -= i;
    }
}
