using BitTorrentClient.Models.Peers;
using BitTorrentClient.BitTorrent.Downloads;
using BitTorrentClient.BitTorrent.Peers;
using BitTorrentClient.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.BitTorrent.Managing;
public class DefaultPeerManagingStrategy 
{
    private readonly int _maxUploaders;
    private readonly int _maxDownloaders;
}
