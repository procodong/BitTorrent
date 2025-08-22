using BitTorrentClient.Helpers.Streams;

namespace BitTorrentClient.Engine.Infrastructure.Storage.Data;
public readonly record struct StreamData(long ByteOffset, long Size, Lazy<Task<IRandomAccesStream>> Handle);
