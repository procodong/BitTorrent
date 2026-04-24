using BitTorrentClient.Core.Presentation.Torrent;
using BitTorrentClient.Engine.Models.Config;

namespace BitTorrentClient.Engine.Models.Downloads;

public readonly record struct Download(
    ReadOnlyMemory<byte> ClientId,
    DownloadData Data,
    DownloadSettings Settings
    );