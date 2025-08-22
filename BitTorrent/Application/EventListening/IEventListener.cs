using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.EventListening;
public interface IEventListener
{
    Task ListenAsync(CancellationToken cancellationToken = default);
}