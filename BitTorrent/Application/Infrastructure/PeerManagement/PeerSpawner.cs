using BitTorrentClient.Models.Messages;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using BitTorrentClient.Application.Infrastructure.Peers;
using BitTorrentClient.Application.Infrastructure.Peers.Exceptions;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Helpers.Streams;
using BitTorrentClient.Application.Infrastructure.Storage.Distribution;
using BitTorrentClient.Protocol.Networking.PeerWire;
using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Protocol.Networking.PeerWire.Handshakes;
using BitTorrentClient.Protocol.Networking.PeerWire.Reading;

namespace BitTorrentClient.Application.Infrastructure.PeerManagement;
public class PeerSpawner
{
    private readonly DownloadState _downloadState;
    private readonly ILogger _logger;
    private readonly ChannelWriter<int?> _peerRemovalWriter;
    private readonly ChannelWriter<RespondedHandshakeHandler> _peerAdderWriter;
    private readonly byte[] _peerId;

    public PeerSpawner(DownloadState downloadState, ILogger logger, ChannelWriter<int?> peerRemovalWriter, ChannelWriter<RespondedHandshakeHandler> peerWriter, byte[] peerId)
    {
        _downloadState = downloadState;
        _logger = logger;
        _peerRemovalWriter = peerRemovalWriter;
        _peerAdderWriter = peerWriter;
        _peerId = peerId;
    }

    public async Task SpawnConnect(PeerAddress address)
    {
        try
        {
            var connection = new TcpClient();
            await connection.ConnectAsync(address.Ip, address.Port);
            var stream = new NetworkStream(connection.Client, true);
            var peerStream = new HandshakeHandler(stream, new BufferCursor(1 << 14 + 13));
            await peerStream.SendHandShakeAsync(new(0, _downloadState.Download.Torrent.OriginalInfoHashBytes, _peerId));
            var responded = await peerStream.ReadHandShakeAsync();
            await peerStream.SendBitfieldAsync(_downloadState.DownloadedPieces);
            if (!responded.ReceivedHandshake.InfoHash.SequenceEqual(_downloadState.Download.Torrent.OriginalInfoHashBytes))
            {
                _logger.LogInformation("Encountered a peer with an invalid info hash");
                return;
            }
            await _peerAdderWriter.WriteAsync(responded);
        }
        catch (Exception ex)
        {
            await _peerRemovalWriter.WriteAsync(default);
            if (ex is not SocketException && ex is not IOException)
            {
                _logger.LogError("connecting to peer", ex);
            }
        }
    }

    public async Task SpawnListener(RespondedHandshakeHandler handshaker, int index, PeerState state, ChannelReader<PeerRelation> relationReader, CancellationToken cancellationToken = default)
    {
        var haveChannel = Channel.CreateBounded<int>(16);
        int downloadWriterIndex;
        lock (_downloadState.Peers)
        {
            downloadWriterIndex = _downloadState.Peers.Add((haveChannel.Writer));
        }
        try
        {
            if (handshaker.SentHandshake is null)
            {
                await handshaker.SendHandShakeAsync(new(0, _downloadState.Download.Torrent.OriginalInfoHashBytes, _peerId), cancellationToken);
                await handshaker.SendBitfieldAsync(_downloadState.DownloadedPieces, cancellationToken);
            }

            var (buffer, stream, messageWriter) = handshaker.Finish();
            var messageStream = new BufferedMessageStream(stream, buffer);
            var reader = new PeerWireReader(messageStream);
            var messagePipe = new Pipe();
            var requestChannel = Channel.CreateBounded<BlockData>(8);
            var cancellationChannel = Channel.CreateBounded<PieceRequest>(8);
            var writingHandler = new PeerMessageWriter(messageWriter, messagePipe.Reader, requestChannel.Reader, cancellationChannel.Reader);
            var peer = new Peer(_downloadState, state, messagePipe.Writer, requestChannel.Writer, cancellationChannel.Writer);
            await using var eventHandler = new PeerEventHandler(reader, peer);
            _ = writingHandler.ListenAsync(cancellationToken);
            await eventHandler.ListenAsync(haveChannel.Reader, relationReader, cancellationToken);
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
