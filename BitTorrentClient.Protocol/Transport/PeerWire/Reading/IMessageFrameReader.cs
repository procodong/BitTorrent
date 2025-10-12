using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Reading;

public interface IMessageFrameReader : IAsyncDisposable
{
    MessageType Type { get; }
    Task<int> ReadHaveAsync(CancellationToken cancellationToken = default);
    Task<BlockRequest> ReadRequestAsync(CancellationToken cancellationToken = default);
    Task<BlockData> ReadPieceAsync(CancellationToken cancellationToken = default);
    Stream ReadStream();
}