namespace BitTorrentClient.Engine.Infrastructure.Peers.Exceptions;
public enum PeerErrorReason
{
    InvalidRequest,
    InvalidPiece,
    InvalidProtocol,
    InvalidPacketSize
}
