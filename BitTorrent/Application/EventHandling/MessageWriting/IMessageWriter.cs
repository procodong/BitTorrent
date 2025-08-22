using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitTorrentClient.Models.Messages;
using BitTorrentClient.Protocol.Networking.PeerWire;
using BitTorrentClient.Protocol.Networking.PeerWire.Sending;

namespace BitTorrentClient.Application.EventHandling.MessageWriting;
public interface IMessageWriter
{
    void WriteRelation(RelationUpdate relation);
    void WriteHave(int piece);
    void WriteRequest(PieceRequest request);
    void WriteCancel(PieceRequest cancel);
    Task<int> WriteBlockAsync(BlockData block, CancellationToken cancellationToken = default);
    Task FlushAsync(CancellationToken cancellationToken = default);
}
