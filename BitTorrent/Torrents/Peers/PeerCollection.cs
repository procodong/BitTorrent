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
    private readonly HashSet<string> _peerIds = [];
    private readonly PeerSpawner _spawner;
    private readonly ILogger _logger;
    private readonly int _pieceCount;

    public int Count => _peers.Count;

    public PeerCollection(PeerSpawner spawner, ILogger logger, int pieceCount)
    {
        _spawner = spawner;
        _logger = logger;
        _pieceCount = pieceCount;
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

    public void Remove(int index)
    {
        _peers[index].Canceller.Cancel();
        _peers.Remove(index);
    }

    public void ConnectAll(IEnumerable<PeerAddress> addresses)
    {
        foreach (PeerAddress peer in addresses)
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
