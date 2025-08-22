namespace BitTorrentClient.Models.Application;

public readonly record struct DownloadData(ReadOnlyMemory<byte> InfoHash, ReadOnlyMemory<byte> PieceHashes, string[][] Trackers, FileData[] Files, int PieceSize, int PieceCount, long Size, string Name, string SavePath);

public readonly record struct FileData(string Path, long Size);