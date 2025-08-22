using BitTorrentClient.Application.EventListening.Peers;
using BitTorrentClient.Models.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.EventHandling.Peers;
internal class PeerEventHandler : IPeerEventHandler
{
    private readonly IPeer _peer;

    public PeerEventHandler(IPeer peer)
    {
        _peer = peer;
    }

    public Task OnBitfieldAsync(Stream bitfield, CancellationToken cancellationToken = default)
    {
    }

    public Task OnCancelAsync(PieceRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task OnClientHaveAsync(int piece, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task OnClientRelationAsync(Relation relation, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task OnPeerHaveAsync(int piece, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task OnPeerRelationAsync(Relation relation, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task OnPieceAsync(BlockData piece, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task OnRequestAsync(PieceRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
