using BencodeNET.Torrents;
using BitTorrentClient.Models.Application;

namespace BitTorrentClient.Application.Infrastructure.Storage.Distribution;

internal record Download(
    string ClientId,
    DownloadData Data,
    Config Config
    );