namespace BitTorrentClient.Application.Events.EventHandling.MessageWriting;

public interface IPieceDelayer
{
    void DelayNextPiece(int milliseconds);
}