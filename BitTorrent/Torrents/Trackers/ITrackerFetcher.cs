using BitTorrent.Models.Trackers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Trackers;
public interface ITrackerFetcher
{
    Task<TrackerResponse> FetchAsync(TrackerUpdate update, CancellationToken cancellationToken = default);
}
