using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Protocol.Networking.Trackers;
public interface ITrackerFinder
{
    Task<ITrackerFetcher> FindTrackerAsync(IEnumerable<IList<string>> urls);
}
