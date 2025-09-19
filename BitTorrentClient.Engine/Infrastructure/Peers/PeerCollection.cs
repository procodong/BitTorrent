using System.Collections;
using BitTorrentClient.Engine.Infrastructure.Peers.Interface;
using BitTorrentClient.Engine.Launchers.Interface;
using BitTorrentClient.Helpers.Utility;
using BitTorrentClient.Protocol.Transport.PeerWire.Connecting.Interface;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Engine.Infrastructure.Peers;
public sealed class PeerCollection : IPeerCollection, IEnumerable<PeerHandle>
{
    private readonly Dictionary<ReadOnlyMemory<byte>, PeerHandle> _peers;
    private readonly PeerConnector _connector;
    private readonly IPeerLauncher _launcher;
    private IEnumerable<IPeerConnector> _potentialPeers = [];
    private IEnumerator<(int Index, IPeerConnector Address)> _peerCursor;
    private int _missedPeers;

    public int Count => _peers.Count;

    public PeerCollection(PeerConnector connector, IPeerLauncher launcher, int maxParallelPeers)
    {
        _peers = new(new MemoryComparer<byte>());
        _connector = connector;
        _launcher = launcher;
        _missedPeers = maxParallelPeers;
        _peerCursor = DefaultValueEnumerator<(int, IPeerConnector)>.Instance;
    }

    public void Add(PeerWireStream stream)
    {
        if (_peers.ContainsKey(stream.ReceivedHandshake.PeerId))
        {
            stream.Dispose();
        }
        _peers.Add(stream.ReceivedHandshake.PeerId, _launcher.LaunchPeer(stream));
    }

    public void Remove(ReadOnlyMemory<byte>? id)
    {
        if (id is not null)
        {
            var peer = _peers[id.Value];
            peer.Canceller.Cancel();
            _peers.Remove(id.Value);
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
        return _peers.Values.GetEnumerator();
    }

    public IEnumerator<PeerHandle> GetEnumerator()
    {
        return _peers.Values.GetEnumerator();
    }

    public void Feed(IEnumerable<IPeerConnector> addresses)
    {
        var oldPeers = _potentialPeers.Take(_peerCursor.Current.Index).ToHashSet();
        _potentialPeers = addresses.Where(old => !oldPeers.Contains(old));
        _peerCursor.Dispose();
        _peerCursor = _potentialPeers.Index().GetEnumerator();
        int i;
        for (i = 0; i < _missedPeers && _peerCursor.MoveNext(); i++)
        {
            _ = _connector.SpawnConnect(_peerCursor.Current.Address);
        }
        _missedPeers -= i;
    }
}
