using BitTorrentClient.Helpers.Parsing;
using System.Net;
using BitTorrentClient.Protocol.Presentation.UdpTracker.Models;

namespace BitTorrentClient.Protocol.Presentation.UdpTracker;
public static class UdpTrackerDecoder
{
    public static UdpTrackerData ReadAnnounceResponse(BigEndianBinaryReader reader)
    {
        var interval = reader.ReadInt32();
        var leechers = reader.ReadInt32();
        var seeders = reader.ReadInt32();
        int peerCount = reader.Remaining / (sizeof(int) + sizeof(short));
        var peers = new IPEndPoint[peerCount];
        for (int i = 0; i < peerCount; i++)
        {
            var ip = reader.ReadBytes(4);
            var port = reader.ReadUInt16();
            peers[i] = new(new IPAddress(ip), port);
        }
        return new(interval, seeders, leechers, peerCount, peers);
    }

    public static TrackerHeader ReadHeader(BigEndianBinaryReader reader)
    {
        var action = reader.ReadInt32();
        var transactionId = reader.ReadInt32();
        return new(action, transactionId);
    }
}
