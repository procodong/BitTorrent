using BitTorrent.Models.Peers;
using BitTorrent.Models.Tracker;
using BitTorrent.Models.Trackers;
using BitTorrent.Utils;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Trackers.UdpTracker;
public static class UdpTrackerDecoder
{
    public static TrackerResponse ReadAnnounceResponse(BigEndianBinaryReader reader)
    {
        var interval = reader.ReadInt32();
        var leechers = reader.ReadInt32();
        var seeders = reader.ReadInt32();
        var peerCount = leechers + seeders;
        var peers = new List<PeerAddress>(peerCount);
        for (int i = 0; i < peerCount; i++)
        {
            var ip = reader.ReadBytes(4);
            var port = reader.ReadInt16();
            peers.Add(new(new(ip), port));
        }
        return new(interval, default, seeders, leechers, peers, default);
    }

    public static TrackerHeader DecodeHeader(BigEndianBinaryReader reader)
    {
        var action = reader.ReadInt32();
        var transactionId = reader.ReadInt32();
        return new(action, transactionId);
    }

    public static long ReadConnectionId(ReadOnlySpan<byte> buffer)
    {
        return BinaryPrimitives.ReadInt64BigEndian(buffer[8..]);
    }
}
