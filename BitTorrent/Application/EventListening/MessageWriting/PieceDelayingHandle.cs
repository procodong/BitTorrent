using BitTorrentClient.Application.EventHandling.MessageWriting;

namespace BitTorrentClient.Application.EventListening.MessageWriting;

public class PieceDelayingHandle : IPieceDelayer
{
    public int Delay { get; private set; }
    public void DelayNextPiece(int milliseconds)
    {
        Delay = milliseconds;
    }
}