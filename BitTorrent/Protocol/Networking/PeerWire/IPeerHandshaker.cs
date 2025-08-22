using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Models.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Protocol.Networking.PeerWire;
public interface IPeerHandshaker
{
    Task SendHandShakeAsync(HandShakeData handShake, CancellationToken cancellationToken = default);
    Task SendBitfieldAsync(LazyBitArray bitfield, CancellationToken cancellationToken = default);
}
