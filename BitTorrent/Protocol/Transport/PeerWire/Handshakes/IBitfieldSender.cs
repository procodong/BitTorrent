using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

public interface IBitfieldSender<TRet>
{
    Task<TRet> SendBitfieldAsync(LazyBitArray bitfield, CancellationToken cancellationToken = default);
}