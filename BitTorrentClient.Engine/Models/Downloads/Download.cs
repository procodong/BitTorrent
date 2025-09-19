using BitTorrentClient.Protocol.Presentation.Torrent;

namespace BitTorrentClient.Engine.Models.Downloads;

public record Download(
    ReadOnlyMemory<byte> ClientId,
    DownloadData Data,
    Config Config
    );