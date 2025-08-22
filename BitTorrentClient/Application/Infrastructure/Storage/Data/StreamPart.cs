namespace BitTorrentClient.Application.Infrastructure.Storage.Data;
internal readonly record struct StreamPart(StreamData StreamData, int Length, long Position);