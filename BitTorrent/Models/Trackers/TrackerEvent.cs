using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Models.Tracker;
public enum TrackerEvent
{
    Started,
    Stopped,
    Completed,
}
