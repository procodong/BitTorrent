using System.Threading.Channels;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Application.Events.Handling.Trackers;
public interface ITrackerListeningHandler
{
    void AddDownload(ReadOnlyMemory<byte> infoHash, ChannelWriter<IHandshakeSender<IBitfieldSender>> channel);
    void RemoveDownload(ReadOnlyMemory<byte> infoHash);
    Task SendPeerAsync(IHandshakeReceiver<IRespondedHandshakeSender<IBitfieldSender>> peer, CancellationToken cancellationToken = default);
}
