using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Application.Input.Commands;
public interface ICommand
{
    Task Run(ICommandContext context);
}
