using BitTorrent.Models.Trackers;
using BitTorrent.Utils;

namespace BitTorrent.Torrents.Trackers.UdpTracker;
public static class UdpTrackerEncoder
{
    private const long CONNECT_CONNECTION_ID = 0x41727101980;

    public static void WriteConnect(BigEndianBinaryWriter writer, int transactionId)
    {
        writer.Write(CONNECT_CONNECTION_ID);
        writer.Write(0);
        writer.Write(transactionId);
    }

    public static void WriteAnnounce(BigEndianBinaryWriter writer, long connectionId, int transactionId, TrackerRequest request)
    {
        writer.Write(connectionId);
        writer.Write(1);
        writer.Write(transactionId);
        writer.Write(request.InfoHash);
        writer.Write(System.Text.Encoding.ASCII.GetBytes(request.ClientId));
        writer.Write(request.Downloaded);
        writer.Write(request.Left);
        writer.Write(request.Uploaded);
        writer.Write((int)request.TrackerEvent);
        writer.Write(0);
        writer.Write(Random.Shared.Next());
        writer.Write(-1);
        writer.Write((short)request.Port);
    }
}
