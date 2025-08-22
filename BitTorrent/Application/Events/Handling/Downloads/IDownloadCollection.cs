using BencodeNET.Torrents;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Application.Infrastructure.Storage.Distribution;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Application.Events.Handling.Downloads;

public interface IDownloadCollection
{
    Task AddDownloadAsync(Torrent torrent, StorageStream storage, string? name, CancellationToken cancellationToken = default);
    Task AddPeerAsync(IHandshakeReceiver<IRespondedHandshakeSender<IBitfieldSender<PeerWireStream>>> peer, CancellationToken cancellationToken = default);
    Task RemoveDownloadAsync(Func<Download, bool> condition);
    IEnumerable<DownloadUpdate> GetUpdates();
}