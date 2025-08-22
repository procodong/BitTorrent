namespace BitTorrentClient.Application.Infrastructure.Downloads;
public readonly record struct PeerManagerHandle(IApplicationUpdateProvider UpdateProvider, CancellationTokenSource Canceller, byte[] InfoHash, IPeerSpawner PeerSpawner);
