using BencodeNET.Torrents;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Application.Infrastructure.Downloads.Interface;

internal interface IDownloadCollection : IDisposable, IAsyncDisposable
{
    Task<IDownloadController> AddDownloadAsync(DownloadData data, StorageStream storage, CancellationToken cancellationToken = default);
    Task AddPeerAsync(IHandshakeReceiver<IRespondedHandshakeSender<IBitfieldSender<PeerWireStream>>> peer, CancellationToken cancellationToken = default);
    Task<bool> RemoveDownloadAsync(ReadOnlyMemory<byte> id);
    IEnumerable<IDownloadController> GetDownloads();
    IDownloadController GetDownloadController(ReadOnlyMemory<byte> id);
}