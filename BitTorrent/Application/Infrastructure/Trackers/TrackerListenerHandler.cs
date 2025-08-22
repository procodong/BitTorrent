using System.Threading.Channels;
using BitTorrentClient.Application.EventHandling.Trackers;
using BitTorrentClient.Helpers;
using BitTorrentClient.Protocol.Networking.PeerWire;

namespace BitTorrentClient.Application.Infrastructure.Trackers;

public class TrackerListenerHandler : ITrackerListeningHandler
{
    private readonly Dictionary<ReadOnlyMemory<byte>, ChannelWriter<RespondedPeerHandshaker>> _downloads;

    public TrackerListenerHandler()
    {
        _downloads = new(new MemoryComparer<byte>());
    }
    
    public void AddDownload(ReadOnlyMemory<byte> infoHash, ChannelWriter<RespondedPeerHandshaker> channel)
    {
        _downloads.Add(infoHash, channel);
    }

    public void RemoveDownload(ReadOnlyMemory<byte> infoHash)
    {
        _downloads.Remove(infoHash);
    }

    public async Task SendPeerAsync(PeerHandshaker peer, CancellationToken cancellationToken = default)
    {
        var responded = await peer.ReadHandShakeAsync(cancellationToken);
        await _downloads[responded.ReceivedHandshake.InfoHash].WriteAsync(responded, cancellationToken);
    }
}