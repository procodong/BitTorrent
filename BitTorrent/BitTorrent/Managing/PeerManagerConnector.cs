namespace BitTorrentClient.BitTorrent.Managing;
public readonly record struct PeerManagerConnector(PeerManager UpdateProvider, CancellationTokenSource Canceller, byte[] InfoHash);
