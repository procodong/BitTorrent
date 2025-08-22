namespace BitTorrentClient.Engine.Infrastructure.MessageWriting.Interface;

internal interface IPieceDelayer
{
    void DelayNextPiece(int milliseconds);
}