namespace BitTorrentClient.Application.Events.Handling.MessageWriting;

public interface IPieceDelayer
{
    void DelayNextPiece(int milliseconds);
}