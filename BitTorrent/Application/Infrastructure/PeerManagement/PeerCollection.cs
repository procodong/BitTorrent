using System.Collections;
using System.Threading.Channels;
using BitTorrentClient.Application.Events.Handling.PeerManagement;
using BitTorrentClient.Application.Infrastructure.Peers;
using BitTorrentClient.Application.Launchers.Peers;
using BitTorrentClient.Helpers;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Protocol.Transport.PeerWire.Connecting;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Application.Infrastructure.PeerManagement;
public class PeerCollection : IPeerCollection, IEnumerable<PeerHandle>
{
    private readonly SlotMap<PeerHandle> _peers = [];
    private readonly HashSet<ReadOnlyMemory<byte>> _peerIds = new(new MemoryComparer<byte>());
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
        _peerCursor = Enumerable.Empty<(int, IPeerConnector)>().GetEnumerator();
    }

    public void Add(PeerWireStream stream)
    {
        byte[] peerId = stream.ReceivedHandshake.PeerId;
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
            _ = _connector.SpawnConnect(_peerCursor.Current.Address).ConfigureAwait(false);
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
            _ = _connector.SpawnConnect(_peerCursor.Current.Address).ConfigureAwait(false);
        }
        _missedPeers -= i;
    }
}
