using System.Runtime.InteropServices;
using BitTorrentClient.Core.Presentation.PeerWire.Models;

namespace BitTorrentClient.Engine.Models.Messages;

[StructLayout(LayoutKind.Explicit)]
public readonly struct MessageUnion
{
    [FieldOffset(0)] public readonly int Have;
    [FieldOffset(0)] public readonly BlockRequest Request;
    [FieldOffset(0)] public readonly BlockShareHeader Block;

    public MessageUnion(int have)
    {
        Have = have;
    }

    public MessageUnion(BlockRequest request)
    {
        Request = request;
    }

    public MessageUnion(BlockShareHeader block)
    {
        Block = block;
    }
}
