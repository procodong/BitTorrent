using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using BitTorrentClient.Engine.Infrastructure.Downloads;
using BitTorrentClient.Engine.Infrastructure.Peers.Interface;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;
using BitTorrentClient.Protocol.Transport.PeerWire.Connecting.Interface;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Engine.Infrastructure.Peers;
public class PeerConnector : IPeerSpawner
{
    private readonly DownloadState _downloadState;
    private readonly ILogger _logger;
    private readonly ChannelWriter<int?> _peerRemovalWriter;
    private readonly ChannelWriter<PeerWireStream> _peerAdderWriter;
    private readonly byte[] _peerId;

    public PeerConnector(DownloadState downloader, ILogger logger, ChannelWriter<int?> peerRemovalWriter, ChannelWriter<PeerWireStream> peerWriter, byte[] peerId)
    {
        _downloadState = downloader;
        _logger = logger;
        _peerRemovalWriter = peerRemovalWriter;
        _peerAdderWriter = peerWriter;
        _peerId = peerId;
    }

    public async Task SpawnConnect(IPeerConnector address, CancellationToken cancellationToken = default)
    {
        try
        {
            var sender = await address.ConnectAsync(cancellationToken);
            try
            {
                var handshakeData = new HandshakeData(0, _downloadState.Download.Data.InfoHash, _peerId);
                var reader = await sender.SendDataAsync(handshakeData, _downloadState.DownloadedPieces, cancellationToken);
                var stream = await reader.ReadDataAsync(cancellationToken);
                if (!stream.ReceivedHandshake.InfoHash.Span.SequenceEqual(_downloadState.Download.Data.InfoHash.Span))
                {
                    _logger.LogInformation("Encountered a peer with an invalid info hash");
                    return;
                }

                await _peerAdderWriter.WriteAsync(stream, cancellationToken);
            }
            catch
            {
                await sender.DisposeAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            await _peerRemovalWriter.WriteAsync(default, cancellationToken);
            if (ex is not SocketException && ex is not IOException)
            {
                _logger.LogError(ex, "connecting to peer {}", ex);
            }
        }
    }

    public async Task SpawnConnect(PendingPeerWireStream<SendDataPhase> peer, CancellationToken cancellationToken = default)
    {
        try
        {
            var handshakeData = new HandshakeData(0, _downloadState.Download.Data.InfoHash,
                Encoding.ASCII.GetBytes(_downloadState.Download.ClientId));
            var stream = await peer.SendDataAsync(handshakeData, _downloadState.DownloadedPieces, cancellationToken);
            await _peerAdderWriter.WriteAsync(stream, cancellationToken);
        }
        catch (Exception ex)
        {
            await peer.DisposeAsync();
            if (ex is not SocketException or IOException)
            {
                _logger.LogError(ex, "connecting to peer {}", ex);
            }
        }
    }
}
