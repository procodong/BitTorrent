using BitTorrentClient.Engine.Models;

namespace BitTorrentClient.Api.PersistentState;

public class ConfigBuilder
{
    public int? TargetDownload { get; set; }
    public int? TargetUpload { get; set; }
    public int? TargetUploadSeeding { get; set; }
    public int? RequestSize { get; set; }
    public int? RequestQueueSize { get; set; }
    public int? MaxRarePieceCount { get; set; }
    public int? PeerUpdateInterval { get; set; }
    public int? MaxRequestSize { get; set; }
    public int? KeepAliveInterval { get; set; }
    public int? ReceiveTimeout { get; set; }
    public int? UiUpdateInterval { get; set; }
    public int? PieceSegmentSize { get; set; }
    public int? MaxParallelPeers { get; set; }
    public int? TransferRateResetInterval { get; set; }


    internal Config Build(Config defaultConfig) => new(
        TargetDownload ?? defaultConfig.TargetDownload,
        TargetUpload ?? defaultConfig.TargetUpload,
        TargetUploadSeeding ?? defaultConfig.TargetUploadSeeding,
        RequestSize ?? defaultConfig.RequestSize,
        RequestQueueSize ?? defaultConfig.RequestQueueSize,
        MaxRarePieceCount ?? defaultConfig.MaxRarePieceCount,
        PeerUpdateInterval ?? defaultConfig.PeerUpdateInterval,
        MaxRequestSize ?? defaultConfig.MaxRequestSize,
        KeepAliveInterval ?? defaultConfig.KeepAliveInterval,
        ReceiveTimeout ?? defaultConfig.ReceiveTimeout,
        UiUpdateInterval ?? defaultConfig.UiUpdateInterval,
        PieceSegmentSize ?? defaultConfig.PieceSegmentSize,
        MaxParallelPeers ?? defaultConfig.MaxParallelPeers,
        TransferRateResetInterval ?? defaultConfig.TransferRateResetInterval
    );
}