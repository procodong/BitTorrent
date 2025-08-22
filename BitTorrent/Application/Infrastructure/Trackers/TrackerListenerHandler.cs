using System.Threading.Channels;
using BitTorrentClient.Application.Events.EventHandling.Trackers;
using BitTorrentClient.Helpers;
using BitTorrentClient.Protocol.Networking.PeerWire;
using BitTorrentClient.Protocol.Networking.PeerWire.Handshakes;

namespace BitTorrentClient.Application.Infrastructure.Trackers;

public class TrackerListenerHandler : ITrackerListeningHandler
{
    private readonly Dictionary<ReadOnlyMemory<byte>, ChannelWriter<IHandshakeSender<IBitfieldSender>>> _downloads;

    public TrackerListenerHandler()
    {
        _downloads = new(new MemoryComparer<byte>());
    }
    
    public void AddDownload(ReadOnlyMemory<byte> infoHash, ChannelWriter<IHandshakeSender<IBitfieldSender>> channel)
    {
        _downloads.Add(infoHash, channel);
    }

    public void RemoveDownload(ReadOnlyMemory<byte> infoHash)
    {
        _downloads.Remove(infoHash);
    }

    public async Task SendPeerAsync(IHandshakeReceiver<IRespondedHandshakeSender<IBitfieldSender>> peer, CancellationToken cancellationToken = default)
    {
        var responded = await peer.ReadHandShakeAsync(cancellationToken);
        await _downloads[responded.ReceiveHandshake.InfoHash].WriteAsync(responded, cancellationToken);
    }
}