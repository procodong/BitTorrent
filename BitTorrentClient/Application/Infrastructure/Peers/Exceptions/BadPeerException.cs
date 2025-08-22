namespace BitTorrentClient.Application.Infrastructure.Peers.Exceptions;
public class BadPeerException(PeerErrorReason reason) : Exception(reason.ToString())
{
    public PeerErrorReason Reason => reason;
}
