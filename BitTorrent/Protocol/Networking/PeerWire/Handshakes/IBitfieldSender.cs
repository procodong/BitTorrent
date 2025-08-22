using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Protocol.Networking.PeerWire.Handshakes;

public interface IBitfieldSender
{
    Task<IHandshakeFinisher> SendBitfieldAsync(LazyBitArray bitfield, CancellationToken cancellationToken = default);
}