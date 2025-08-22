using BencodeNET.Torrents;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Application.Infrastructure.Interfaces;

public interface IDownloadCollection : IDisposable, IAsyncDisposable
{
    Task AddDownloadAsync(Torrent torrent, StorageStream storage, string? name, CancellationToken cancellationToken = default);
    Task AddPeerAsync(IHandshakeReceiver<IRespondedHandshakeSender<IBitfieldSender<PeerWireStream>>> peer, CancellationToken cancellationToken = default);
    Task<bool> RemoveDownloadAsync(ReadOnlyMemory<byte> id);
    IEnumerable<DownloadUpdate> GetDownloadState();
}