using BitTorrentClient.Models.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Protocol.Presentation.PeerWire;
public interface IMessageSender
{
    void SendRelation(Relation relation);
    void SendHave(int piece);
    void SendRequest(PieceRequest request);
    void SendCancel(PieceRequest cancel);
    Task SendBlockAsync(BlockData block, CancellationToken cancellationToken = default);
    Task FlushAsync(CancellationToken cancellationToken = default);
}
