using BitTorrentClient.Application.Infrastructure.Downloads.Interface;
using BitTorrentClient.Application.Infrastructure.Peers.Interface;
using BitTorrentClient.Application.Infrastructure.Storage.Distribution;

namespace BitTorrentClient.Application.Infrastructure.Downloads;
internal readonly record struct PeerManagerHandle(IDownloadController Controller, CancellationTokenSource Canceller, IPeerSpawner PeerSpawner, Download Download);
