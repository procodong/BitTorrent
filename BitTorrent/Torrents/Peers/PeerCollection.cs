using BitTorrentClient.Models.Messages;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Torrents.Downloads;
using BitTorrentClient.Utils;
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

namespace BitTorrentClient.Torrents.Peers;
public class PeerCollection : IEnumerable<PeerConnector>
{
    private readonly SlotMap<PeerConnector> _peers = [];
    private readonly HashSet<ReadOnlyMemory<byte>> _peerIds = new(new MemoryComparer<byte>());
    private readonly PeerSpawner _spawner;
    private readonly int _pieceCount;
    private List<PeerAddress> _potentialPeers = [];
    private IEnumerator<(int Index, PeerAddress Address)> _peerCursor = Enumerable.Empty<(int, PeerAddress)>().GetEnumerator();
    private int _missedPeers;

    public int Count => _peers.Count;

    public PeerCollection(PeerSpawner spawner, int pieceCount, int maxParallelPeers)
    {
        _spawner = spawner;
        _pieceCount = pieceCount;
        _missedPeers = maxParallelPeers;
    }

    public void Add(IdentifiedPeerWireStream stream)
    {
        if (_peerIds.Contains(stream.PeerId))
        {
            ConnectNext();
            return;
        }
        _peerIds.Add(stream.PeerId);
        var eventChannel = Channel.CreateBounded<PeerRelation>(16);
        var state = new PeerState(new(_pieceCount));
        var peerConnector = new PeerConnector(state, eventChannel.Writer, new(), new(), new());
        int index = _peers.Add(peerConnector);
        _ = _spawner.SpawnListener(stream.Stream, index, state, eventChannel.Reader, peerConnector.Canceller.Token).ConfigureAwait(false);
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
