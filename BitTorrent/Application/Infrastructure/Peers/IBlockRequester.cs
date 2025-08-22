using BitTorrentClient.BitTorrent.Downloads;
using BitTorrentClient.Models.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.Infrastructure.Peers;
public interface IBlockRequester
{
    void CancelAllDownloads();
    void RequestUpload(PieceRequest request);
    bool TryRequestDownload(out Block block);
}
