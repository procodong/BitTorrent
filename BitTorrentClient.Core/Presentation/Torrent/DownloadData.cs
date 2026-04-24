namespace BitTorrentClient.Core.Presentation.Torrent;

public readonly record struct FileData(string Path, long Size);

public record class DownloadData(ReadOnlyMemory<byte> InfoHash, ReadOnlyMemory<byte> PieceHashes, Uri[][] Trackers, FileData[] Files, int PieceSize, int PieceCount, long Size, string Name, string SavePath);
