using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.BitTorrent.Trackers.Errors;
public class TrackerException(string message) : Exception(message)
{
}
