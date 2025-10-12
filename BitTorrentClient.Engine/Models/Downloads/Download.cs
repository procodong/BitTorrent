using BitTorrentClient.Protocol.Presentation.Torrent;

namespace BitTorrentClient.Engine.Models.Downloads;

public readonly record struct Download(
    ReadOnlyMemory<byte> ClientId,
    DownloadData Data,
    Config Config
    );