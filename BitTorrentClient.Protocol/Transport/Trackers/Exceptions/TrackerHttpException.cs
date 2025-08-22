namespace BitTorrentClient.Protocol.Transport.Trackers.Exceptions;
public class TrackerHttpException(int code) : Exception($"Tracker fetch failed with HTTP error code {code}")
{
    public readonly int Code = code;
}
