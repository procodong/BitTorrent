using System.Runtime.CompilerServices;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Engine.Models;

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
    int TransferRateResetInterval,
    int PeerBufferSize
)
{
    public static Config Default => new(
        TargetDownload: int.MaxValue,
        TargetUpload: 100_000,
        TargetUploadSeeding: 10_000_000,
        RequestSize: 1 << 14,
        RequestQueueSize: 5,
        MaxRarePieceCount: 20,
        PeerUpdateInterval: 10 * 1000,
        MaxRequestSize: 1 << 17,
        KeepAliveInterval: 90 * 1000,
        ReceiveTimeout: 2 * 60 * 1000,
        UiUpdateInterval: 1000,
        PieceSegmentSize: 1 << 17,
        MaxParallelPeers: 30,
        TransferRateResetInterval: 10,
        PeerBufferSize: (1 << 14) + Unsafe.SizeOf<BlockShareHeader>() + sizeof(int) + sizeof(byte)
        );
}