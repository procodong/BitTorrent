namespace BitTorrent.Torrents.Managing;
public readonly record struct PeerManagerConnector(IUpdateProvider UpdateProvider, TaskCompletionSource Completion, byte[] InfoHash);
