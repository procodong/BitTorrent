using BitTorrentClient.Models.Peers;
using BitTorrentClient.Models.Trackers;
using BitTorrentClient.Helpers.Parsing;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Protocol.Presentation.UdpTracker;
public static class UdpTrackerDecoder
{
    public static UdpTrackerData ReadAnnounceResponse(BigEndianBinaryReader reader)
    {
        var interval = reader.ReadInt32();
        var leechers = reader.ReadInt32();
        var seeders = reader.ReadInt32();
        int peerCount = reader.Remaining / 6;
        var peers = ReadAddresses(reader, peerCount);
        return new(interval, seeders, leechers, peerCount, peers);
    }

    private static IEnumerable<PeerAddress> ReadAddresses(BigEndianBinaryReader reader, int count)
    {
        for (int i = 0; i < count; i++)
        {

            var ip = reader.ReadBytes(4);
            var port = reader.ReadUInt16();
            yield return (new(new(ip), port));
        }
    } 

    public static TrackerHeader ReadHeader(BigEndianBinaryReader reader)
    {
        var action = reader.ReadInt32();
        var transactionId = reader.ReadInt32();
        return new(action, transactionId);
    }
}
