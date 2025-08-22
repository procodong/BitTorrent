namespace BitTorrentClient.Application.Infrastructure.Storage.Distribution;
public class BlockCursor
{
    private readonly Block _block;
    private int _position;

    public PieceDownload Piece => _block.Piece;
    public int Remaining => _block.Length - _position;
    public int Position => _position;

    public BlockCursor(Block block)
    {
        _block = block;
    }

    public Block GetRequest(int requestSize)
    {
        int size = int.Min(_block.Length - _position, requestSize);
        var block = new Block(_block.Piece, _block.Begin + _position, size);
        _position += size;
        return block;
    }
}
