using BitTorrentClient.BitTorrent.Managing;

namespace BitTorrentClient.Application.Infrastructure.Downloads;
public readonly record struct PeerManagerHandle(PeerManager UpdateProvider, CancellationTokenSource Canceller, byte[] InfoHash);
