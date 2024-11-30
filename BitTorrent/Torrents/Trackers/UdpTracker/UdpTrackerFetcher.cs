using BitTorrent.Models.Tracker;
using BitTorrent.Models.Trackers;
using BitTorrent.Torrents.Trackers.Errors;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace BitTorrent.Torrents.Trackers.UdpTracker;
public class UdpTrackerFetcher : ITrackerFetcher
{
    private readonly UdpClient _client;
    private readonly Dictionary<int, byte[]> _receivedMessages = [];
    private readonly Random _random;
    private readonly byte[] _buffer;
    private readonly int _port;
    private long? _connectionId;

    public UdpTrackerFetcher(UdpClient client, int port)
    {
        _client = client;
        _random = new();
        _buffer = new byte[100];
        _port = port;
    }

    public async Task<TrackerResponse> FetchAsync(TrackerUpdate update)
    {
        var request = new TrackerRequest(update.InfoHash, update.ClientId, _port, update.DataTransfer.Upload, update.DataTransfer.Download, update.Left, update.TrackerEvent);
        int transaction = await SendAnnounceAsync(request);
        var response = await ReceiveAsync(transaction);
        var stream = new MemoryStream(response)
        {
            Position = 8
        };
        TrackerResponse responseData = UdpTrackerDecoder.ReadAnnounceResponse(new(stream));
        return responseData;
    }

    private async Task<int> SendAnnounceAsync(TrackerRequest request) 
    {
        var stream = new MemoryStream(_buffer);
        var writer = new UdpTrackerWriter(stream);
        int transactionId = _random.Next();
        writer.WriteAnnounce(_connectionId!.Value, transactionId, request);
        await _client.SendAsync(_buffer.AsMemory(0, (int)stream.Position));
        return transactionId;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        int transactionId = await SendConnectAsync(cancellationToken);
        var response = await ReceiveAsync(transactionId, cancellationToken);
        _connectionId = UdpTrackerDecoder.ReadConnectionId(response);
    }

    private async Task<int> SendConnectAsync(CancellationToken cancellationToken = default)
    {
        int transactionId = _random.Next();
        var stream = new MemoryStream(_buffer);
        var writer = new UdpTrackerWriter(stream);
        writer.WriteConnect(transactionId);
        await _client.SendAsync(_buffer.AsMemory(0, (int)stream.Position), cancellationToken);
        return transactionId;
    }

    private async Task<byte[]> ReceiveAsync(int transactionId, CancellationToken cancellationToken = default)
    {
        if (_receivedMessages.TryGetValue(transactionId, out var message))
        {
            return message;
        }
        int resends = 0;
        while (true)
        {
            Task<UdpReceiveResult> receiveTask = _client.ReceiveAsync(cancellationToken).AsTask();
            var ready = await Task.WhenAny(receiveTask, Task.Delay(60 * (1 << resends) * 1000, cancellationToken));
            if (ready != receiveTask)
            {
                resends++;
                await SendConnectAsync(cancellationToken);
                continue;
            }
            var receive = await receiveTask;
            var header = UdpTrackerDecoder.DecodeHeader(new(new MemoryStream(receive.Buffer)));
            if (header.Action == 3)
            {
                var errorMessage = UdpTrackerDecoder.ReadErrorMessage(receive.Buffer);
                throw new TrackerException(errorMessage);
            }
            if (header.TransactionId == transactionId)
            {
                return receive.Buffer;
            }
            else if (header.Action == 0)
            {
                _connectionId = UdpTrackerDecoder.ReadConnectionId(receive.Buffer);
            }
            else
            {
                _receivedMessages[header.TransactionId] = receive.Buffer;
            }
        }
    }
}
