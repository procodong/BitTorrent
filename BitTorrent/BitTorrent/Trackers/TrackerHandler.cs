using BitTorrentClient.Models.Peers;
using BitTorrentClient.Models.Trackers;
using BitTorrentClient.BitTorrent.Peers;
using BitTorrentClient.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using BitTorrentClient.BitTorrent.Peers.Connections;
using BitTorrentClient.Helpers.Extensions;

namespace BitTorrentClient.BitTorrent.Trackers;
public class TrackerHandler
{
    private readonly Dictionary<ReadOnlyMemory<byte>, ChannelWriter<PeerHandshaker>> _eventSenders;
    private readonly TcpListener _listener;
    private readonly ILogger _logger;

    public TrackerHandler(int port, ILogger logger)
    {
        _listener = new(IPAddress.Any, port);
        _logger = logger;
        _eventSenders = new(new MemoryComparer<byte>());
    }

    public async Task ListenAsync(ChannelReader<PeerReceivingSubscribe> downloadReceiver)
    {
        _listener.Start();
        Task<TcpClient> clientTask = _listener.AcceptTcpClientAsync();
        Task<PeerReceivingSubscribe> newDownloadTask = downloadReceiver.ReadAsync().AsTask();
        while (true)
        {
            var ready = await Task.WhenAny(clientTask, newDownloadTask);
            if (ready == clientTask)
            {
                try
                {
                    TcpClient client = await clientTask;
                    await CreateClient(client);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error responding to peer: {}", ex);
                }
                finally
                {
                    clientTask = _listener.AcceptTcpClientAsync();
                }
            }
            else if (ready == newDownloadTask)
            {
                try
                {
                    PeerReceivingSubscribe download = await newDownloadTask;
                    if (download.EventWriter is null)
                    {
                        _eventSenders.Remove(download.InfoHash);
                    }
                    else
                    {
                        _eventSenders[download.InfoHash] = download.EventWriter;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error adding or removing a download from tracker handler: {}", ex);
                }
                finally
                {
                    newDownloadTask = downloadReceiver.ReadAsync().AsTask();
                }
            }
        }
    }

    private async Task CreateClient(TcpClient client)
    {
        var stream = new NetworkStream(client.Client, true);
        var handshaker = new PeerHandshaker(stream, 1 << 14 + 13);
        var handshake = await handshaker.ReadHandShakeAsync();
        await _eventSenders[handshake.InfoHash].WriteAsync(handshaker);
    }
}
