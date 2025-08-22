using BencodeNET.Torrents;
using BitTorrentClient.Models.Application;

namespace BitTorrentClient.Application.Infrastructure.Storage.Distribution;

public record Download(
    int DownloadIndex,
    string ClientId,
    string Name,
    Torrent Torrent,
    Config Config
    );