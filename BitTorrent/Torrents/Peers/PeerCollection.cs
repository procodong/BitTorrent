using BitTorrent.Models.Messages;
using BitTorrent.Models.Peers;
using BitTorrent.Torrents.Downloads;
using BitTorrent.Torrents.Managing;
using BitTorrent.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Peers;
public class PeerCollection : IEnumerable<PeerConnector>
{
    private readonly SlotMap<PeerConnector> _peers = [];
    private readonly HashSet<ReadOnlyMemory<byte>> _peerIds = new(new MemoryComparer<byte>());
    private readonly PeerSpawner _spawner;
    private readonly ILogger _logger;
    private readonly int _pieceCount;
    private readonly int _maxParallelPeers;
    private List<PeerAddress> _potentialPeers = [];
    private int _peerCursor;

    public int Count => _peers.Count;

    public PeerCollection(PeerSpawner spawner, ILogger logger, int pieceCount, int maxParallelPeers)
    {
        _spawner = spawner;
        _logger = logger;
        _pieceCount = pieceCount;
        _maxParallelPeers = maxParallelPeers;
    }

    public void Add(IdentifiedPeerWireStream stream)
    {
        if (_peerIds.Contains(stream.PeerId)) return;
        _peerIds.Add(stream.PeerId);
        var eventChannel = Channel.CreateBounded<PeerRelation>(16);
        var state = new SharedPeerState(new(_pieceCount));
        var peerConnector = new PeerConnector(state, eventChannel.Writer, new(), new(), new());
        int index = _peers.Add(peerConnector);
        _ = _spawner.StartPeer(stream.Stream, index, state, eventChannel.Reader, peerConnector.Canceller.Token);
    }

    public async Task RemoveAsync(int? index)
    {
        if (index is not null)
        {
            await _peers[index.Value].Canceller.CancelAsync();
            _peers.Remove(index.Value);
        }
        if (_peerCursor < _potentialPeers.Count)
        {
            Connect(_potentialPeers[_peerCursor]);
            _peerCursor++;
        }
    }

    public void ConnectAll(List<PeerAddress> addresses)
    {
        _potentialPeers = addresses;
        int maxPeers = int.Min(addresses.Count, _maxParallelPeers);
        _peerCursor = maxPeers + 1;
        foreach (PeerAddress peer in addresses.Take(maxPeers))
        {
            Connect(peer);
        }
    }

    public void Connect(PeerAddress address)
    {
        _ = _spawner.ConnectPeer(address);
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
