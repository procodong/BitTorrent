using BitTorrentClient.Application.Events.EventHandling.MessageWriting;

namespace BitTorrentClient.Application.Events.EventListening.MessageWriting;

public class PieceDelayingHandle : IPieceDelayer
{
    public int Delay { get; private set; }
    public void DelayNextPiece(int milliseconds)
    {
        Delay = milliseconds;
    }
}