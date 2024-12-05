using BitTorrent.Models.Trackers;
using BitTorrent.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Trackers.UdpTracker;
public struct UdpTrackerWriter
{
    private const long CONNECT_CONNECTION_ID = 0x41727101980;
    private readonly BigEndianBinaryWriter _writer;
    public readonly Stream Stream => _writer.BaseStream;

    public UdpTrackerWriter(Stream stream)
    {
        _writer = new(stream);
    }

    public readonly void WriteConnect(int transactionId)
    {
        _writer.Write(CONNECT_CONNECTION_ID);
        _writer.Write(0);
        _writer.Write(transactionId);
    }

    public readonly void WriteAnnounce(long connectionId, int transactionId, TrackerRequest request)
    {
        _writer.Write(connectionId);
        _writer.Write(1);
        _writer.Write(transactionId);
        _writer.Write(request.InfoHash);
        _writer.Write(System.Text.Encoding.ASCII.GetBytes(request.ClientId));
        _writer.Write(request.Downloaded);
        _writer.Write(request.Left);
        _writer.Write(request.Uploaded);
        _writer.Write((int)request.TrackerEvent);
        _writer.Write(0);
        _writer.Write(Random.Shared.Next());
        _writer.Write(-1);
        _writer.Write((short)request.Port);
    }
}
