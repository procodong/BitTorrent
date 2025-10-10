using BitTorrentClient.Helpers.Streams;

namespace BitTorrentClient.Engine.Storage.Data;
public readonly record struct StreamData(long ByteOffset, long Size, Lazy<Task<IRandomAccesStream>> Stream);
