using BitTorrentClient.BitTorrent.Peers.Connections;
using BitTorrentClient.BitTorrent.Peers.Errors;
using BitTorrentClient.Models.Messages;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.BitTorrent.Downloads;
using BitTorrentClient.BitTorrent.Peers.Streaming;
using BitTorrentClient.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrentClient.BitTorrent.Peers;
public class PeerSpawner
{
    private readonly Download _download;
    private readonly ILogger _logger;
    private readonly ChannelWriter<int?> _peerRemovalWriter;
    private readonly ChannelWriter<IdentifiedPeerWireStream> _peerAdderWriter;
    private readonly byte[] _peerId;

    public PeerSpawner(Download download, ILogger logger, ChannelWriter<int?> peerRemovalWriter, ChannelWriter<IdentifiedPeerWireStream> peerWriter, byte[] peerId)
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
            var peerStream = new PeerWireStream(stream);
            await peerStream.SendHandShakeAsync(GetBitField(), _download.Torrent.OriginalInfoHashBytes, _peerId);
            HandShake receivedHandshake = await peerStream.ReadHandShakeAsync();
            if (!receivedHandshake.InfoHash.SequenceEqual(_download.Torrent.OriginalInfoHashBytes))
            {
                _logger.LogInformation("Encountered a peer with an invalid info hash");
                return;
            }
            await _peerAdderWriter.WriteAsync(new(receivedHandshake.PeerId, peerStream));
        }
        catch (Exception ex)
        {
            await _peerRemovalWriter.WriteAsync(default);
            if (ex is not SocketException && ex is not EndOfStreamException && ex is not IOException)
            {
                _logger.LogError("connecting to peer", ex);
            }
        }
    }

    public async Task SpawnListener(PeerWireStream stream, int index, PeerState state, ChannelReader<PeerRelation> relationReader, CancellationToken cancellationToken = default)
    {
        var haveChannel = Channel.CreateBounded<int>(16);
        int downloadWriterIndex = _download.AddPeer(haveChannel.Writer);
        try
        {
            if (!stream.HandShaken)
            {
                await stream.SendHandShakeAsync(GetBitField(), _download.Torrent.OriginalInfoHashBytes, _peerId);
            }
            await using var peer = new PeerEventHandler(stream, _download, state);
            await peer.ListenAsync(haveChannel.Reader, relationReader, cancellationToken);
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

    private ZeroCopyBitArray? GetBitField() => _download.HasDownloadedPieces ? _download.DownloadedPieces : null;
}
