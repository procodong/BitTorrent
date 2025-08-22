using System.Threading.Channels;
using BitTorrentClient.Engine.Infrastructure.Peers.Interface;
using BitTorrentClient.Engine.Infrastructure.Storage.Distribution;
using BitTorrentClient.Engine.Models.Downloads;

namespace BitTorrentClient.Engine.Infrastructure.Downloads;
public readonly record struct PeerManagerHandle(DownloadState State, ChannelWriter<DownloadExecutionState> StateWriter, CancellationTokenSource Canceller, IPeerSpawner PeerSpawner, Download Download);
