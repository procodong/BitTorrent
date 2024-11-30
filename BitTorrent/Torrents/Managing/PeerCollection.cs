using BitTorrent.Models.Messages;
using BitTorrent.Models.Peers;
using BitTorrent.Torrents.Downloads;
using BitTorrent.Torrents.Managing.Events;
using BitTorrent.Torrents.Peers;
using BitTorrent.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Managing;
public class PeerCollection : IEnumerable<PeerConnector>
{
    private readonly SlotMap<PeerConnector> _peers = [];
    private readonly HashSet<PeerAddress> _peerAddresses = [];
    private readonly ILogger _logger;
    private readonly ChannelWriter<IPeerRegisterationEvent> _peerRegisterationWriter;

    public int Count => _peers.Count;

    public PeerCollection(ChannelWriter<IPeerRegisterationEvent> peerRegisterationWriter, ILogger logger)
    {
        _peerRegisterationWriter = peerRegisterationWriter;
        _logger = logger;
    }

    public int Add(PeerConnector peer)
    {
        return _peers.Add(peer);
    }

    public async Task ConnectPeerAsync(PeerAddress address)
    {
        if (_peerAddresses.Contains(address)) return;
        try
        {
            _peerAddresses.Add(address);
            var connection = new TcpClient();
            await connection.ConnectAsync(address.Ip, address.Port);
            var stream = new NetworkStream(connection.Client, true);
            var peerStream = new PeerWireStream(stream);
            await _peerRegisterationWriter.WriteAsync(new PeerAddEvent(peerStream));
        }
        catch (Exception ex)
        {
            _logger.LogError("Error connection to peer: {}", ex.Message);
        }
    }

    public async Task QueueRemoval(int index)
    {
        await _peerRegisterationWriter.WriteAsync(new PeerRemovalEvent(index));
    }

    public void Remove(int index)
    {
        var peer = _peers[index];
        peer.RelationEventWriter.Complete();
        _peers.Remove(index);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_peers).GetEnumerator();
    }

    public IEnumerator<PeerConnector> GetEnumerator()
    {
        return ((IEnumerable<PeerConnector>)_peers).GetEnumerator();
    }
}
