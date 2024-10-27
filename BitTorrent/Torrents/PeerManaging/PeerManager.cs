using BitTorrent.Models.Peers;
using BitTorrent.Torrents.Peers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.PeerManaging;
public class PeerManager(string peerId)
{
    private readonly List<PeerConnector> _peerStats = [];
    private readonly string _peerId = peerId;

    public void AddPeer(TcpClient connection)
    {
        var channel = Channel.CreateUnbounded<PeerManagerEvent>();

    }
}
