namespace BitTorrentClient.Protocol.Transport.Trackers;
public interface ITrackerFinder
{
    Task<ITrackerFetcher> FindTrackerAsync(IEnumerable<IList<string>> urls);
}
