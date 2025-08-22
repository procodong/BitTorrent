using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Data;

public class DownloadUpdate
{
    private readonly BitTorrentClient.Engine.Models.Downloads.DownloadUpdate _update;

    internal DownloadUpdate(BitTorrentClient.Engine.Models.Downloads.DownloadUpdate update)
    {
        _update = update;
    }
    
    public DownloadExecutionState ExecutionState => (DownloadExecutionState)_update.ExecutionState;
    public long Size => _update.Size;
    public double Progress => _update.Progress;
    public string DownloadName => _update.DownloadName;
    public DataTransferVector Transfer => _update.Transfer;
    public DataTransferVector TransferRate => _update.TransferRate;
    public ReadOnlyMemory<byte> Identifier => _update.Identifier;
}