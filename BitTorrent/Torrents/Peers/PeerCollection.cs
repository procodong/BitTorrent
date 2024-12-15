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
using System.Runtime.InteropServices;
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
    private IEnumerator<(int Index, PeerAddress Address)> _peerCursor = new List<(int, PeerAddress)>().GetEnumerator();
    private int _missedPeers;

    public int Count => _peers.Count;

    public PeerCollection(PeerSpawner spawner, ILogger logger, int pieceCount, int maxParallelPeers)
    {
        _spawner = spawner;
        _logger = logger;
        _pieceCount = pieceCount;
        _maxParallelPeers = maxParallelPeers;
        _missedPeers = _maxParallelPeers;
    }

    public void Add(IdentifiedPeerWireStream stream)
    {
        if (_peerIds.Contains(stream.PeerId))
        {
            _missedPeers++;
            return;
        }
        _peerIds.Add(stream.PeerId);
        var eventChannel = Channel.CreateBounded<PeerRelation>(16);
        var state = new SharedPeerState(new(_pieceCount));
        var peerConnector = new PeerConnector(state, eventChannel.Writer, new(), new(), new());
        int index = _peers.Add(peerConnector);
        _ = _spawner.SpawnListener(stream.Stream, index, state, eventChannel.Reader, peerConnector.Canceller.Token);
    }

    public async Task RemoveAsync(int? index)
    {
        if (index is not null)
        {
            await _peers[index.Value].Canceller.CancelAsync();
            _peers.Remove(index.Value);
        }
        if (_peerCursor.MoveNext())
        {
            _ = _spawner.SpawnConnect(_peerCursor.Current.Address);
        }
        else
        {
            _missedPeers++;
        }
    }

    public void Update(List<PeerAddress> addresses)
    {
        var newPeers = new List<PeerAddress>(addresses.Count);
        var oldPeers = _potentialPeers.Take(_peerCursor.Current.Index).ToHashSet();
        foreach (PeerAddress peer in addresses)
        {
            if (!oldPeers.Contains(peer))
            {
                newPeers.Add(peer);
            }
        }
        _potentialPeers = newPeers;
        _peerCursor = newPeers.Indexed().GetEnumerator();
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
