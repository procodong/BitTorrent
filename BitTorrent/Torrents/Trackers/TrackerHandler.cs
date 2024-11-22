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
    private readonly ChannelReader<TrackerUpdate> _requestReader;
    private readonly SlotMap<ChannelWriter<TrackerHandlerEvent>> _eventSenders = [];
    private readonly Dictionary<ReadOnlyMemory<byte>, int> _infoHashToIndex = new(new MemoryComparer<byte>());
    private readonly TcpListener _listener;
    private readonly HttpClient _httpClient;
    private readonly int _clientTimeout;
    private readonly int _port;

    public TrackerHandler(int port, int clientTimeout, HttpClient httpClient, ChannelReader<TrackerUpdate> requestReader)
    {
        _requestReader = requestReader;
        _listener = new(IPAddress.Any, port);
        _port = port;
        _clientTimeout = clientTimeout;
        _httpClient = httpClient;
    }

    public async Task ListenAsync()
    {
        Task<TrackerUpdate> requestTask = _requestReader.ReadAsync().AsTask();
        Task<TcpClient> clientTask = _listener.AcceptTcpClientAsync();
        while (true)
        {
            var ready = await Task.WhenAny(clientTask, requestTask);
            if (ready == requestTask)
            {
                var update = requestTask.Result;
                await UpdateTrackerAsync(update);
                requestTask = _requestReader.ReadAsync().AsTask();
            }
            else if (ready == clientTask)
            {
                TcpClient client = clientTask.Result;
                await CreateClient(client);
                clientTask = _listener.AcceptTcpClientAsync();
            }
        }
    }

    private async Task CreateClient(TcpClient client)
    {
        client.ReceiveTimeout = _clientTimeout;
        var stream = new NetworkStream(client.Client, true);
        var peerStream = new PeerWireStream(stream);
        var handshake = await peerStream.ReadHandShakeAsync();
        var index = _infoHashToIndex[handshake.InfoHash];
        await _eventSenders[index].WriteAsync(new(handshake.PeerId, peerStream));
    }

    private async Task UpdateTrackerAsync(TrackerUpdate update)
    {
        var request = new TrackerRequest(
            InfoHash: update.InfoHash,
            ClientId: update.ClientId,
            Port: _port,
            Uploaded: update.DataTransfer.Uploaded,
            Downloaded: update.DataTransfer.Downloaded,
            Left: update.Left,
            TrackerEvent: update.TrackerEvent
            );
        TrackerResponse response = await TrackerFetcher.Fetch(_httpClient, update.TrackerUrl, request);
        await _eventSenders[update.DownloadId].WriteAsync(new(response));
    }
}
