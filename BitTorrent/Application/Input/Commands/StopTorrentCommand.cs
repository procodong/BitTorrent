using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Application.Input.Commands;
public class StopTorrentCommand(int index) : ICommand
{
    private readonly int _index = index;
    public async Task Run(ICommandContext context)
    {
        await context.RemoveTorrent(_index);
    }
}
