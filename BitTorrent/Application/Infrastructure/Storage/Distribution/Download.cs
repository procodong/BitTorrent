using BencodeNET.Torrents;
using BitTorrentClient.Models.Application;

namespace BitTorrentClient.Application.Infrastructure.Storage.Distribution;

public record Download(
    string ClientId,
    string Name,
    Torrent Torrent,
    Config Config
    );