using BitTorrentClient.Application.Infrastructure.Storage.Distribution;
using BitTorrentClient.Models.Application;
using System.Threading.Channels;

namespace BitTorrentClient.Application.Infrastructure.Downloads;
public readonly record struct PeerManagerHandle(IApplicationUpdateProvider UpdateProvider, ChannelWriter<DownloadExecutionState> StateWriter, CancellationTokenSource Canceller, byte[] InfoHash, IPeerSpawner PeerSpawner, Download Download);
