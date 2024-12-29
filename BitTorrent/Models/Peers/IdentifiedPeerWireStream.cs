using BitTorrentClient.Torrents.Peers.Streaming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Models.Peers;
public readonly record struct IdentifiedPeerWireStream(byte[] PeerId, PeerWireStream Stream);