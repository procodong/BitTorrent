using BitTorrent.Models.Tracker;
using BitTorrent.Torrents.Peers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Trackers;
public class TrackerHandlerEvent
{
    private readonly TrackerHandlerEventType _type;
    private readonly object _data;
    public TrackerResponse Response => (TrackerResponse)_data;
    public (string Id, PeerWireStream Stream) Stream => ((string, PeerWireStream))_data;
    public TrackerHandlerEventType Type => _type;

    public TrackerHandlerEvent(TrackerResponse response)
    {
        _data = response;
        _type = TrackerHandlerEventType.TrackerResponse;
    }

    public TrackerHandlerEvent(string id, PeerWireStream stream)
    {
        _data = (id, stream);
        _type = TrackerHandlerEventType.Stream;
    }
}
