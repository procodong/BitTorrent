using BencodeNET.Torrents;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Application.Events.Handling.Downloads;

public interface IDownloadCollection
{
    Task AddDownloadAsync(Torrent torrent, DownloadStorage storage, CancellationToken cancellationToken = default);
    Task AddPeerAsync(IHandshakeReceiver<IRespondedHandshakeSender<IBitfieldSender<PeerWireStream>>> peer);
    Task RemoveDownloadAsync(ReadOnlyMemory<byte> index);
    IEnumerable<DownloadUpdate> GetUpdates();
}