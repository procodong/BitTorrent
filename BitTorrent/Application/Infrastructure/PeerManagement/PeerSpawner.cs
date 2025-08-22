using BitTorrentClient.Models.Peers;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Threading.Channels;
using BitTorrentClient.Application.Infrastructure.Peers;
using BitTorrentClient.Application.Infrastructure.Peers.Exceptions;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Application.Infrastructure.Downloads;
using BitTorrentClient.Protocol.Transport.PeerWire.Connecting;
using System.Text;
using BitTorrentClient.Application.Launchers.Peers;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using BitTorrentClient.Application.Infrastructure.Storage.Distribution;

namespace BitTorrentClient.Application.Infrastructure.PeerManagement;
public class PeerSpawner : IPeerSpawner
{
    private readonly Downloader _downloader;
    private readonly ILogger _logger;
    private readonly ChannelWriter<int?> _peerRemovalWriter;
    private readonly ChannelWriter<PeerWireStream> _peerAdderWriter;
    private readonly IPeerLauncher _launcher;
    private readonly byte[] _peerId;

    public PeerSpawner(Downloader downloader, IPeerLauncher launcher, ILogger logger, ChannelWriter<int?> peerRemovalWriter, ChannelWriter<PeerWireStream> peerWriter, byte[] peerId)
    {
        _downloader = downloader;
        _launcher = launcher;
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
            var bitfieldSender = await handshake.SendHandShakeAsync(new(0, _downloader.Torrent.OriginalInfoHashBytes, _peerId), cancellationToken);
            var reader = await bitfieldSender.SendBitfieldAsync(_downloader.DownloadedPieces, cancellationToken);
            var responded = await reader.ReadHandShakeAsync(cancellationToken);
            if (!responded.ReceivedHandshake.InfoHash.SequenceEqual(_downloader.Torrent.OriginalInfoHashBytes))
            {
                _logger.LogInformation("Encountered a peer with an invalid info hash");
                return;
            }
            await _peerAdderWriter.WriteAsync(responded, cancellationToken);
        }
        catch (Exception ex)
        {
            await _peerRemovalWriter.WriteAsync(default, cancellationToken);
            if (ex is not SocketException && ex is not IOException)
            {
                _logger.LogError("connecting to peer", ex);
            }
        }
    }

    public async Task SpawnConnect(IRespondedHandshakeSender<IBitfieldSender<PeerWireStream>> peer, CancellationToken cancellationToken = default)
    {
        try
        {
            var bitfieldSender = await peer.SendHandShakeAsync(new(0, _downloader.Torrent.OriginalInfoHashBytes, Encoding.ASCII.GetBytes(_downloader.ClientId)), cancellationToken);
            var stream = await bitfieldSender.SendBitfieldAsync(_downloader.DownloadedPieces, cancellationToken);
            await _peerAdderWriter.WriteAsync(stream, cancellationToken);
        }
        catch (Exception ex)
        {
            if (ex is not SocketException or IOException)
            {
                _logger.LogError("connecting to peer", ex);
            }
        }
    }

    public async Task SpawnListener(PeerWireStream stream, int index, PeerState state, ChannelReader<PeerRelation> relationReader, CancellationToken cancellationToken = default)
    {
        var haveChannel = Channel.CreateBounded<int>(16);
        var requester = new BlockDistributor(_downloader);
        int downloadWriterIndex;
        lock (_downloader.Peers)
        {
            downloadWriterIndex = _downloader.Peers.Add(haveChannel.Writer);
        }
        try
        {
            await _launcher.LaunchPeer(stream, state, requester, relationReader, haveChannel.Reader, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (ex is BadPeerException)
            {
                _logger.LogError("peer connection", ex);
            }
            else if (ex is not SocketException or IOException)
            {
                _logger.LogError("peer connection", ex);
            }
        }
        finally
        {
            lock (_downloader.Peers)
            {
                _downloader.Peers.Remove(downloadWriterIndex);
            }
            await _peerRemovalWriter.WriteAsync(index, cancellationToken);
        }
    }
}
