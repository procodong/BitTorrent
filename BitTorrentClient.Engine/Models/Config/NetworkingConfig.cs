namespace BitTorrentClient.Engine.Models.Config;

public class NetworkingConfig
{
    public int RequestSize { get; init; }
    public int RequestQueueSize { get; init; }
    public int MaxRequestSize { get; init; }
    public int PieceSegmentSize { get; init; }
    public int PeerBufferSize { get; init; }
    public int PiecesBufferSize { get; init; }
    public TimeSpan ReceiveTimeout { get; init; }
    public TimeSpan PeerUpdateInterval { get; init; }
    public TimeSpan KeepAliveInterval { get; init; }
    public TimeSpan TransferRateResetInterval { get; init; }
}
