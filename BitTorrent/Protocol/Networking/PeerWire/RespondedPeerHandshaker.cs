using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Models.Messages;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Protocol.Networking.PeerWire;
public class RespondedPeerHandshaker : IPeerHandshaker
{
    private readonly PeerHandshaker _handshaker;

    internal RespondedPeerHandshaker(PeerHandshaker handshaker)
    {
        _handshaker = handshaker;
    }

    public HandShakeData ReceivedHandshake => _handshaker.ReceivedHandShake!.Value;
    public HandShakeData? SentHandshake => _handshaker.SentHandShake;

    public Task SendBitfieldAsync(LazyBitArray bitfield, CancellationToken cancellationToken = default)
    {
        return _handshaker.SendBitfieldAsync(bitfield, cancellationToken);
    }

    public Task SendHandShakeAsync(HandShakeData handShake, CancellationToken cancellationToken = default)
    {
        return _handshaker.SendHandShakeAsync(handShake, cancellationToken);
    }
    public (BufferCursor, Stream, PipeWriter) Finish() => _handshaker.Finish();
}
