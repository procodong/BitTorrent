using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Application.Input;
public interface ICommandContext
{
    Task AddTorrent(string torrentPath, string targetPath);
    Task RemoveTorrent(int index);
}
