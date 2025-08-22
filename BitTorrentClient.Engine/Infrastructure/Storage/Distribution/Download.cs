using BitTorrentClient.Engine.Models;
using BitTorrentClient.Protocol.Presentation.Torrent;

namespace BitTorrentClient.Engine.Infrastructure.Storage.Distribution;

public record Download(
    string ClientId,
    DownloadData Data,
    Config Config
    );