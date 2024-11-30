using BitTorrent.Models.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Managing;
public interface IUpdateProvider
{
    DownloadUpdate GetUpdate();
}
