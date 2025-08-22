using BitTorrentClient.Application.Infrastructure.Interfaces;

namespace BitTorrentClient.Application.Events.Listening.MessageWriting;

public class PieceDelayingHandle : IPieceDelayer
{
    public bool Changed { get; set; }
    public int Delay { get; private set; } = -1;
    public void DelayNextPiece(int milliseconds)
    {
        Delay = milliseconds;
        Changed = true;
    }

    public void Reset()
    {
        Delay = -1;
    }
}