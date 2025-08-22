using BitTorrentClient.Models.Peers;
using BitTorrentClient.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.BitTorrent.Peers.Connections;
public class PeerState(LazyBitArray ownedPieces)
{
    public PeerRelation RelationToMe = new();
    public PeerRelation Relation = new();
    public LazyBitArray OwnedPieces = ownedPieces;
    public DataTransferCounter DataTransfer = new();
    public TaskCompletionSource Completion = new();
}