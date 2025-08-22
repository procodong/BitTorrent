using BitTorrentClient.Protocol.Networking.PeerWire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using BitTorrentClient.Protocol.Networking.PeerWire.Handshakes;

namespace BitTorrentClient.Application.Events.EventHandling.Trackers;
public interface ITrackerListeningHandler
{
    void AddDownload(ReadOnlyMemory<byte> infoHash, ChannelWriter<IHandshakeSender<IBitfieldSender>> channel);
    void RemoveDownload(ReadOnlyMemory<byte> infoHash);
    Task SendPeerAsync(IHandshakeReceiver<IRespondedHandshakeSender<IBitfieldSender>> peer, CancellationToken cancellationToken = default);
}
