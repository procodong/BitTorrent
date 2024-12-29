using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.Input;
public interface ICommandContext
{
    Task AddTorrentAsync(string torrentPath, string targetPath);
    Task RemoveTorrentAsync(int index);
}
