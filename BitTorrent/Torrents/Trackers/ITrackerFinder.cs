using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Trackers;
public interface ITrackerFinder
{
    Task<ITrackerFetcher> FindTrackerAsync(IEnumerable<IList<string>> urls);
}
