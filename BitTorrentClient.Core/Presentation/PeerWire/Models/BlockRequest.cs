namespace BitTorrentClient.Core.Presentation.PeerWire.Models;
public readonly record struct BlockRequest(int Index, int Begin, int Length)
{
    public static implicit operator BlockShareHeader(BlockRequest req) => new(req.Index, req.Begin);
}