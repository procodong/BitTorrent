namespace BitTorrentClient.Application.Infrastructure.Storage.Data;
public readonly record struct StreamPart(StreamData StreamData, int Length, long Position);