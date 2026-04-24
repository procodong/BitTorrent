using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Engine.Models.Config;

public class DownloadSettings
{
    public DataTransferVector TargetDataTransferPerSecond { get; init; }
    public int MaxParallelPeers { get; init; }
    public PieceSelectionStrategyType Strategy { get; init; }
}

public enum PieceSelectionStrategyType
{
    RarestFirst,
    Sequential
}