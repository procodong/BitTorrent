using BitTorrentClient.Helpers.Streams;

namespace BitTorrentClient.Application.Infrastructure.Storage.Data;
internal readonly record struct StreamData(long ByteOffset, long Size, Lazy<Task<IRandomAccesStream>> Handle);
