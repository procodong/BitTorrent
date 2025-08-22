using System.Threading.Channels;
using BitTorrentClient.Engine.Infrastructure.Downloads;

namespace BitTorrentClient.Engine.Models.Downloads;

public readonly record struct DownloadHandle(DownloadState State, ChannelWriter<DownloadExecutionState> Writer);