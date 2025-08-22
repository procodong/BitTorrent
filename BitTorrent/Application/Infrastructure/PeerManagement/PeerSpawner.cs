using BitTorrentClient.Models.Messages;
using BitTorrentClient.Models.Peers;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Threading.Channels;
using BitTorrentClient.Application.Infrastructure.Peers;
using BitTorrentClient.Application.Infrastructure.Peers.Exceptions;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Protocol.Networking.PeerWire.Handshakes;
using BitTorrentClient.Application.Infrastructure.Downloads;
using BitTorrentClient.Protocol.Transport.PeerWire.Connecting;
using System.Text;

namespace BitTorrentClient.Application.Infrastructure.PeerManagement;
public class PeerSpawner : IPeerSpawner
{
    private readonly DownloadState _downloadState;
    private readonly ILogger _logger;
    private readonly ChannelWriter<int?> _peerRemovalWriter;
    private readonly ChannelWriter<PeerWireStream> _peerAdderWriter;
    private readonly byte[] _peerId;

    public PeerSpawner(DownloadState downloadState, ILogger logger, ChannelWriter<int?> peerRemovalWriter, ChannelWriter<PeerWireStream> peerWriter, byte[] peerId)
    {
        _downloadState = downloadState;
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
            var bitfieldSender = await handshake.SendHandShakeAsync(new(0, _downloadState.Download.Torrent.OriginalInfoHashBytes, _peerId), cancellationToken);
            var reader = await bitfieldSender.SendBitfieldAsync(_downloadState.DownloadedPieces, cancellationToken);
            var responded = await reader.ReadHandShakeAsync(cancellationToken);
            if (!responded.ReceivedHandshake.InfoHash.SequenceEqual(_downloadState.Download.Torrent.OriginalInfoHashBytes))
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
            var bitfieldSender = await peer.SendHandShakeAsync(new(0, _downloadState.Download.Torrent.OriginalInfoHashBytes, Encoding.ASCII.GetBytes(_downloadState.Download.ClientId)), cancellationToken);
            var stream = await bitfieldSender.SendBitfieldAsync(_downloadState.DownloadedPieces, cancellationToken);
            await _peerAdderWriter.WriteAsync(stream, cancellationToken);
        }
        catch (Exception ex)
        {
            if (ex is not SocketException && ex is not IOException)
            {
                _logger.LogError("connecting to peer", ex);
            }
        }
    }

    public async Task SpawnListener(PeerWireStream stream, int index, PeerState state, ChannelReader<PeerRelation> relationReader, CancellationToken cancellationToken = default)
    {
        var haveChannel = Channel.CreateBounded<int>(16);
        int downloadWriterIndex;
        lock (_downloadState.Peers)
        {
            downloadWriterIndex = _downloadState.Peers.Add(haveChannel.Writer);
        }
        try
        {
            var requestChannel = Channel.CreateBounded<BlockData>(8);
            var cancellationChannel = Channel.CreateBounded<PieceRequest>(8);
            var peer = new Peer(state, );
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (ex is BadPeerException)
            {
                _logger.LogError("peer connection", ex);
            }
        }
        finally
        {
            lock (_downloadState.Peers)
            {
                _downloadState.Peers.Remove(downloadWriterIndex);
            }
            await _peerRemovalWriter.WriteAsync(index, cancellationToken);
        }
    }
}
