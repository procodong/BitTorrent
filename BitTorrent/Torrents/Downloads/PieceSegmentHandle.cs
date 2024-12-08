using BitTorrent.Models.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Downloads;
public class PieceSegmentHandle
{
    private readonly int _offset;
    private readonly int _size;
    private readonly PieceDownload _piece;
    private int _position;

    public PieceDownload Piece => _piece;
    public PieceRequest Request => new(_piece.PieceIndex, _offset, _size);
    public int Remaining => _size - _position;
    public int Position => _position;

    public PieceSegmentHandle(PieceDownload piece, int offset, int size)
    {
        _offset = offset;
        _size = size;
        _piece = piece;
    }

    public PieceRequest GetRequest(int requestSize)
    {
        int size = int.Min(_size - _position, requestSize);
        var request = new PieceRequest(_piece.PieceIndex, _offset + _position, size);
        _position += size;
        return request;
    }
}
