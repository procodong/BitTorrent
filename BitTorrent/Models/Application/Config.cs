namespace BitTorrentClient.Models.Application;
public record class Config(
    int TargetDownload,
    int TargetUpload,
    int TargetUploadSeeding,
    int RequestSize,
    int RequestQueueSize,
    int MaxRarePieceCount,
    int PeerUpdateInterval,
    int MaxRequestSize,
    int KeepAliveInterval,
    int ReceiveTimeout,
    int UiUpdateInterval,
    int PieceSegmentSize,
    int MaxParallelPeers,
    int TransferRateResetInterval
    );