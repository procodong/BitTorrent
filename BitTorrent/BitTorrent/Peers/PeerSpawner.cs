using BitTorrentClient.BitTorrent.Peers.Connections;
using BitTorrentClient.BitTorrent.Peers.Errors;
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
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Helpers.Streams;
using BitTorrentClient.Application.Infrastructure.Storage.Distribution;

namespace BitTorrentClient.BitTorrent.Peers;
public class PeerSpawner
{
    private readonly Download _download;
    private readonly ILogger _logger;
    private readonly ChannelWriter<int?> _peerRemovalWriter;
    private readonly ChannelWriter<PeerHandshaker> _peerAdderWriter;
    private readonly byte[] _peerId;

    public PeerSpawner(Download download, ILogger logger, ChannelWriter<int?> peerRemovalWriter, ChannelWriter<PeerHandshaker> peerWriter, byte[] peerId)
    {
        _download = download;
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
            var peerStream = new PeerHandshaker(stream, 1 << 14 + 13);
            await peerStream.SendHandShakeAsync(new(0, _download.Torrent.OriginalInfoHashBytes, _peerId));
            HandShake receivedHandshake = await peerStream.ReadHandShakeAsync();
            await peerStream.SendBitfieldAsync(_download.DownloadedPieces);
            if (!receivedHandshake.InfoHash.SequenceEqual(_download.Torrent.OriginalInfoHashBytes))
            {
                _logger.LogInformation("Encountered a peer with an invalid info hash");
                return;
            }
            await _peerAdderWriter.WriteAsync(peerStream);
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

    public async Task SpawnListener(PeerHandshaker handshaker, int index, PeerState state, ChannelReader<PeerRelation> relationReader, CancellationToken cancellationToken = default)
    {
        var haveChannel = Channel.CreateBounded<int>(16);
        int downloadWriterIndex = _download.AddPeer(haveChannel.Writer);
        try
        {
            if (handshaker.SentHandShake is null)
            {
                await handshaker.SendHandShakeAsync(new(0, _download.Torrent.OriginalInfoHashBytes, _peerId));
                await handshaker.SendBitfieldAsync(_download.DownloadedPieces);
            }

            var (buffer, stream, messageWriter) = handshaker.Finish();
            var messageStream = new BufferedMessageStream(stream, buffer);
            var reader = new PeerWireReader(messageStream);
            var messagePipe = new Pipe();
            var requestChannel = Channel.CreateBounded<BlockData>(8);
            var cancellationChannel = Channel.CreateBounded<PieceRequest>(8);
            var writingHandler = new PeerMessageWriter(messageWriter, messagePipe.Reader, requestChannel.Reader, cancellationChannel.Reader);
            var peer = new Peer(_download, state, messagePipe.Writer, requestChannel.Writer, cancellationChannel.Writer);
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
            _download.RemovePeer(downloadWriterIndex);
            await _peerRemovalWriter.WriteAsync(index, cancellationToken);
        }
    }
}
