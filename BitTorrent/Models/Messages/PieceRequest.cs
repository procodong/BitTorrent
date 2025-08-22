namespace BitTorrentClient.Models.Messages;
public readonly record struct PieceRequest(int Index, int Begin, int Length)
{
    public static implicit operator PieceShareHeader(PieceRequest req) => new(req.Index, req.Begin);
}