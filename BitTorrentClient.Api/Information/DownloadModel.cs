using BitTorrentClient.Protocol.Presentation.Torrent;

namespace BitTorrentClient.Api.Information;

public readonly struct DownloadModel
{
    internal DownloadData Data { get; }
    
    public ReadOnlyMemory<byte> Identifier => Data.InfoHash;
    public string Name => Data.Name;
    
    internal DownloadModel(DownloadData data)
    {
        Data = data;
    }
}
    