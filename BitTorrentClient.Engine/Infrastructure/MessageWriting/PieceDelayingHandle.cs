using BitTorrentClient.Engine.Infrastructure.MessageWriting.Interface;

namespace BitTorrentClient.Engine.Infrastructure.MessageWriting;

public sealed class PieceDelayingHandle : IPieceDelayer
{
    public int DelayMilliSeconds { get; private set; }
    public bool Changed { get; set; }
    public void DelayNextPiece(int milliseconds)
    {
        DelayMilliSeconds = milliseconds;
        Changed = true;
    }

    public void Reset()
    {
        DelayMilliSeconds = -1;
    }
}