using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Models.Application;
public enum DownloadExecutionState
{
    Running,
    PausedByUser,
    PausedAutomatically,
}
