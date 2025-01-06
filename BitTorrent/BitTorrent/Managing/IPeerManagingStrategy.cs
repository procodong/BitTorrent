using BitTorrentClient.BitTorrent.Peers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.BitTorrent.Managing;
public interface IPeerManagingStrategy
{
    (BitArray Downloaders, BitArray Uploaders) GetPeerRelations(List<PeerConnector> peers);
}
