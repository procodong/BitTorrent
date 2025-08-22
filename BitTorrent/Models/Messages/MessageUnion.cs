using System.Runtime.InteropServices;

namespace BitTorrentClient.Models.Messages;

[StructLayout(LayoutKind.Explicit)]
public struct MessageUnion
{
    [FieldOffset(0)] public readonly int Have;
    [FieldOffset(0)] public readonly BlockRequest Request;
    [FieldOffset(0)] public readonly PieceShareHeader Piece;

    public MessageUnion(int have)
    {
        Have = have;
    }

    public MessageUnion(BlockRequest request)
    {
        Request = request;
    }

    public MessageUnion(PieceShareHeader piece)
    {
        Piece = piece;
    }
}
