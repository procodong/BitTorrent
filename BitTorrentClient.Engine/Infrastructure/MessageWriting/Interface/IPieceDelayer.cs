namespace BitTorrentClient.Engine.Infrastructure.MessageWriting.Interface;

public interface IPieceDelayer
{
    void DelayNextPiece(int milliseconds);
}