using BitTorrentClient.Core.Presentation.Torrent;
using BitTorrentClient.Engine.Models.Config;

namespace BitTorrentClient.Api.Information;

public sealed class DownloadModel
{
    internal DownloadData Data { get; }
    internal DownloadSettings Settings { get; }

    public ReadOnlyMemory<byte> Identifier => Data.InfoHash;
    public string Name => Data.Name;

    internal DownloadModel(DownloadData data, DownloadSettings settings)
    {
        Data = data;
        Settings = settings;
    }
}
