using BitTorrentClient.Models.Peers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Models.Application;
public readonly record struct DownloadUpdate(string DownloadName, DataTransferVector Transfered, DataTransferVector TransferRate, long Size);
