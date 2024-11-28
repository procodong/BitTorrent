using BitTorrent.Models.Peers;
using BitTorrent.Models.Tracker;
using BitTorrent.Models.Trackers;
using BitTorrent.Torrents.Peers;
using BitTorrent.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Trackers;
public class TrackerHandler
{
    private readonly ChannelReader<PeerReceivingSubscribe> _downloadReceiver;
    private readonly Dictionary<ReadOnlyMemory<byte>, ChannelWriter<IdentifiedPeerWireStream>> _eventSenders = new(new MemoryComparer<byte>());
    private readonly TcpListener _listener;
    private readonly int _clientReceiveTimeout;

    public TrackerHandler(int port, int clientReceiveTimeout, ChannelReader<PeerReceivingSubscribe> downloadReceiver)
    {
        _listener = new(IPAddress.Any, port);
        _clientReceiveTimeout = clientReceiveTimeout;
        _downloadReceiver = downloadReceiver;
    }

    public async Task ListenAsync()
    {
        Task<TcpClient> clientTask = _listener.AcceptTcpClientAsync();
        Task<PeerReceivingSubscribe> newDownloadTask = _downloadReceiver.ReadAsync().AsTask();
        while (true)
        {
            var ready = await Task.WhenAny(clientTask, newDownloadTask);
            if (ready == clientTask)
            {
                TcpClient client = await clientTask;
                await CreateClient(client);
                clientTask = _listener.AcceptTcpClientAsync();
            }
            else if (ready == newDownloadTask)
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
                newDownloadTask = _downloadReceiver.ReadAsync().AsTask();
            }
        }
    }

    private async Task CreateClient(TcpClient client)
    {
        client.ReceiveTimeout = _clientReceiveTimeout;
        var stream = new NetworkStream(client.Client, true);
        var peerStream = new PeerWireStream(stream);
        var handshake = await peerStream.ReadHandShakeAsync();
        await _eventSenders[handshake.InfoHash].WriteAsync(new(handshake.PeerId, peerStream));
    }
}
