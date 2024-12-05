namespace BitTorrent.Torrents.Managing;
public readonly record struct PeerManagerConnector(PeerManager UpdateProvider, CancellationTokenSource CancellationTokenSource, byte[] InfoHash);
