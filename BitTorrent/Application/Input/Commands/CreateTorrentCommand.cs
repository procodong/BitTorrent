using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Application.Input.Commands;
public class CreateTorrentCommand(string torrentPath, string targetPath) : ICommand
{
    private readonly string _torrentPath = torrentPath;
    private readonly string _targetPath = targetPath;
    public async Task Run(ICommandContext context)
    {
        await context.AddTorrent(_torrentPath, _targetPath);
    }
}
