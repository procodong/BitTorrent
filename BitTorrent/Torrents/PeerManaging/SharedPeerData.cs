using BitTorrent.Models.Peers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.PeerManaging;
public class SharedPeerData(BitArray ownedPieces)
{
    public PeerRelation RelationToMe = new();
    public PeerRelation Relation = new();
    public BitArray OwnedPieces = ownedPieces;
    public DataTransferCounter Stats = new();
}