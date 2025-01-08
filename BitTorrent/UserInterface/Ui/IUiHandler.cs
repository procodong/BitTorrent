using BitTorrentClient.Models.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.Ui;
public interface IUiHandler
{
    void Update(IEnumerable<DownloadUpdate> updates);
}
