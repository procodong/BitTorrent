namespace BitTorrentClient.Application.EventHandling.MessageWriting;

public interface IPieceDelayer
{
    void DelayNextPiece(int milliseconds);
}