using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Threading.Channels;
using BitTorrentClient.Application.Infrastructure.Downloads;
using BitTorrentClient.Protocol.Transport.PeerWire.Connecting;
using System.Text;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using BitTorrentClient.Application.Infrastructure.Peers.Interface;

namespace BitTorrentClient.Application.Infrastructure.Peers;
internal class PeerConnector : IPeerSpawner
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
            var handshake = await address.ConnectAsync(cancellationToken);
            var disposer = handshake.GetDisposer();
            try
            {
                var bitfieldSender = await handshake.SendHandShakeAsync(
                    new(0, _downloadState.Download.Data.InfoHash, _peerId), cancellationToken);
                var reader = await bitfieldSender.SendBitfieldAsync(_downloadState.DownloadedPieces, cancellationToken);
                var responded = await reader.ReadHandShakeAsync(cancellationToken);
                if (!responded.ReceivedHandshake.InfoHash.Span.SequenceEqual(_downloadState.Download.Data.InfoHash.Span))
                {
                    _logger.LogInformation("Encountered a peer with an invalid info hash");
                    return;
                }

                await _peerAdderWriter.WriteAsync(responded, cancellationToken);
            }
            catch
            {
                await disposer.DisposeAsync();
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

    public async Task SpawnConnect(IRespondedHandshakeSender<IBitfieldSender<PeerWireStream>> peer, CancellationToken cancellationToken = default)
    {
        var disposer = peer.GetDisposer();
        try
        {
            var bitfieldSender = await peer.SendHandShakeAsync(new(0, _downloadState.Download.Data.InfoHash, Encoding.ASCII.GetBytes(_downloadState.Download.ClientId)), cancellationToken);
            var stream = await bitfieldSender.SendBitfieldAsync(_downloadState.DownloadedPieces, cancellationToken);
            await _peerAdderWriter.WriteAsync(stream, cancellationToken);
        }
        catch (Exception ex)
        {
            await disposer.DisposeAsync();
            if (ex is not SocketException or IOException)
            {
                _logger.LogError(ex, "connecting to peer {}", ex);
            }
        }
    }
}
