using BitTorrentClient.Helpers.Streams;

namespace BitTorrentClient.Application.Infrastructure.Storage.Data;
public readonly record struct StreamData(long ByteOffset, long Size, Lazy<Task<IRandomAccesStream>> Handle);
