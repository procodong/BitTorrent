namespace BitTorrentClient.Application.Infrastructure.Interfaces;

public interface IPieceDelayer
{
    void DelayNextPiece(int milliseconds);
}