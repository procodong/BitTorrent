namespace BitTorrentClient.Application.Infrastructure.Peers.Exceptions;
public enum PeerErrorReason
{
    InvalidRequest,
    InvalidPiece,
    InvalidProtocol,
    InvalidPacketSize
}
