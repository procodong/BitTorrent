using System.Net.Sockets;
using System.Threading.Channels;
using BitTorrentClient.Engine.Infrastructure.Peers.Interface;
using BitTorrentClient.Engine.Models.Downloads;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;
using BitTorrentClient.Protocol.Transport.PeerWire.Connecting.Interface;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Engine.Infrastructure.Peers;
public sealed class PeerConnector : IPeerSpawner
{
    private readonly Download _download;
    private readonly LazyBitArray _downloadedPieces;
    private readonly ILogger _logger;
    private readonly ChannelWriter<ReadOnlyMemory<byte>?> _peerRemovalWriter;
    private readonly ChannelWriter<PeerWireStream> _peerAdderWriter;

    public PeerConnector(Download downloader, LazyBitArray downloadedPieces, ChannelWriter<ReadOnlyMemory<byte>?> peerRemovalWriter, ChannelWriter<PeerWireStream> peerWriter, ILogger logger)
    {
        _downloadedPieces = downloadedPieces;
        _download = downloader;
        _logger = logger;
        _peerRemovalWriter = peerRemovalWriter;
        _peerAdderWriter = peerWriter;
    }

    public async Task SpawnConnect(IPeerConnector address, CancellationToken cancellationToken = default)
    {
        try
        {
            var sender = await address.ConnectAsync(cancellationToken);
            try
            {
                var handshakeData = new HandshakeData(0, _download.Data.InfoHash, _download.ClientId);
                var reader = await sender.SendDataAsync(handshakeData, _downloadedPieces, cancellationToken);
                var stream = await reader.ReadDataAsync(cancellationToken);
                if (!stream.ReceivedHandshake.InfoHash.Span.SequenceEqual(_download.Data.InfoHash.Span))
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
            var handshakeData = new HandshakeData(0, _download.Data.InfoHash, _download.ClientId);
            var stream = await peer.SendDataAsync(handshakeData, _downloadedPieces, cancellationToken);
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
