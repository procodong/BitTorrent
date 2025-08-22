namespace BitTorrentClient.Application.Infrastructure.MessageWriting.Interface;

internal interface IPieceDelayer
{
    void DelayNextPiece(int milliseconds);
}