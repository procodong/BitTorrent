using BitTorrentClient.Models.Messages;

namespace BitTorrentClient.Protocol.Networking.PeerWire;

public interface IMessageFrameReader
{
    MessageType Type { get; }
    Task<int> ReadHaveAsync(CancellationToken cancellationToken = default);
    Task<PieceRequest> ReadRequestAsync(CancellationToken cancellationToken = default);
    Task<BlockData> ReadPieceAsync(CancellationToken cancellationToken = default);
    Stream ReadStream();
}