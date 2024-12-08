using BitTorrent.Models.Messages;
using BitTorrent.Models.Peers;
using BitTorrent.Torrents.Downloads;
using BitTorrent.Torrents.Peers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Managing;
public class PeerSpawner
{
    private readonly Download _download;
    private readonly ILogger _logger;
    private readonly ChannelWriter<int> _peerRemovalWriter;
    private readonly ChannelWriter<IdentifiedPeerWireStream> _peerAdderWriter;
    private readonly string _peerId;

    public PeerSpawner(Download download, ILogger logger, ChannelWriter<int> peerRemovalWriter, ChannelWriter<IdentifiedPeerWireStream> peerWriter, string peerId)
    {
        _download = download;
        _logger = logger;
        _peerRemovalWriter = peerRemovalWriter;
        _peerAdderWriter = peerWriter;
        _peerId = peerId;
    }

    public async Task ConnectPeer(PeerAddress address)
    {
        try
        {
            var connection = new TcpClient();
            await connection.ConnectAsync(address.Ip, address.Port);
            var stream = new NetworkStream(connection.Client, true);
            var peerStream = new PeerWireStream(stream);
            await peerStream.SendHandShake(_download.DownloadedPiecesCount != 0 ? _download.DownloadedPieces : null, _download.Torrent.OriginalInfoHashBytes, _peerId);
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
            _logger.LogError("Error connecting to peer: {}", ex);
        }
    }

    public async Task StartPeer(PeerWireStream stream, int index, SharedPeerState state, ChannelReader<PeerRelation> relationReader, CancellationToken cancellationToken = default)
    {
        var haveChannel = Channel.CreateUnbounded<int>();
        int downloadWriterIndex = _download.AddPeer(haveChannel.Writer);
        try
        {
            if (!stream.HandShaken)
            {
                await stream.SendHandShake(_download.DownloadedPiecesCount != 0 ? _download.DownloadedPieces : null, _download.Torrent.OriginalInfoHashBytes, _peerId);
            }
            await using var peer = new Peer(stream, haveChannel.Reader, relationReader, _download, state);
            await peer.ListenAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in peer connection: {}", ex);
        }
        finally
        {
            _download.RemovePeer(downloadWriterIndex);
            await _peerRemovalWriter.WriteAsync(index, cancellationToken);
        }
    }
}
