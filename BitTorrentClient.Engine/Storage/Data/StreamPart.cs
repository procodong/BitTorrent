namespace BitTorrentClient.Engine.Storage.Data;
public readonly record struct StreamPart(StreamData StreamData, int Length, long Position);