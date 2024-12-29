using BitTorrentClient.Models.Peers;
using BitTorrentClient.Models.Trackers;
using BitTorrentClient.Utils.Parsing;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Torrents.Trackers.UdpTracker;
public static class UdpTrackerDecoder
{
    public static TrackerResponse ReadAnnounceResponse(BigEndianBinaryReader reader)
    {
        var interval = reader.ReadInt32();
        var leechers = reader.ReadInt32();
        var seeders = reader.ReadInt32();
        int remainingBytes = reader.Length - reader.Position;
        int peerCount = remainingBytes / 6;
        var peers = new List<PeerAddress>(peerCount);
        while (peers.Count < peerCount)
        {
            var ip = reader.ReadBytes(4);
            var port = reader.ReadUInt16();
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

    public static string ReadErrorMessage(ReadOnlySpan<byte> message)
    {
        return System.Text.Encoding.UTF8.GetString(message[8..]);
    }
}
