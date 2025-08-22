using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Protocol.Networking.PeerWire.Handshakes;

public interface IBitfieldSender<TRet>
{
    Task<TRet> SendBitfieldAsync(LazyBitArray bitfield, CancellationToken cancellationToken = default);
}