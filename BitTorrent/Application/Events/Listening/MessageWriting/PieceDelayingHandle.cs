using BitTorrentClient.Application.Events.Handling.MessageWriting;

namespace BitTorrentClient.Application.Events.Listening.MessageWriting;

public class PieceDelayingHandle : IPieceDelayer
{
    public int Delay { get; private set; } = -1;
    public void DelayNextPiece(int milliseconds)
    {
        Delay = milliseconds;
    }

    public void Reset()
    {
        Delay = -1;
    }
}