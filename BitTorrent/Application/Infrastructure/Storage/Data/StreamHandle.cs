namespace BitTorrentClient.Application.Infrastructure.Storage.Data;
public readonly record struct StreamHandle(SemaphoreSlim Lock, Stream Stream);