using BitTorrentClient.Protocol.Networking.PeerWire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.EventHandling.Trackers;
public interface ITrackerListeningHandler
{
    void AddDownload(ReadOnlyMemory<byte> infoHash, ChannelWriter<RespondedPeerHandshaker> channel);
    void RemoveDownload(ReadOnlyMemory<byte> infoHash);
    Task SendPeerAsync(RespondedPeerHandshaker peer, CancellationToken cancellationToken = default);
}
