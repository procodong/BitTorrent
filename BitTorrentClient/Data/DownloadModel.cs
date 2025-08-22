using BitTorrentClient.Protocol.Presentation.Torrent;

namespace BitTorrentClient.Data;

public class DownloadModel
{
    internal DownloadData Data { get; }
    
    public ReadOnlyMemory<byte> Identifier => Data.InfoHash;
    public string Name => Data.Name;
    
    internal DownloadModel(DownloadData data)
    {
        Data = data;
    }
}
    