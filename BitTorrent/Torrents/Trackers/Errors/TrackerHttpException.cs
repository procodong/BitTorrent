using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Trackers.Errors;
public class TrackerHttpException(int code) : Exception($"Tracker fetch failed with HTTP error code {code}")
{
    public readonly int Code = code;
}
