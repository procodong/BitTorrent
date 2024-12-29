using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Models.Trackers;
public enum TrackerEvent
{
    None = 0,
    Completed = 1,
    Started = 2,
    Stopped = 3,
}
