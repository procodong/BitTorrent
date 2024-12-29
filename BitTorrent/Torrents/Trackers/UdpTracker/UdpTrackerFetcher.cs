using BitTorrentClient.Models.Trackers;
using BitTorrentClient.Torrents.Trackers.Errors;
using BitTorrentClient.Utils;
using BitTorrentClient.Utils.Parsing;
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

namespace BitTorrentClient.Torrents.Trackers.UdpTracker;
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
        _buffer = new byte[1 << 7];
        _port = port;
    }

    public async Task<TrackerResponse> FetchAsync(TrackerUpdate update, CancellationToken cancellationToken = default)
    {
        var request = new TrackerRequest(update.InfoHash, update.ClientId, _port, update.DataTransfer.Upload, update.DataTransfer.Download, update.Left, update.TrackerEvent);
        var response = await ReceiveAsync(() => SendAnnounceAsync(request, cancellationToken), cancellationToken);
        TrackerResponse responseData = UdpTrackerDecoder.ReadAnnounceResponse(new(response, 8));
        return responseData;
    }

    private async Task<int> SendAnnounceAsync(TrackerRequest request, CancellationToken cancellationToken = default) 
    {
        int transactionId = _random.Next();
        var writer = new BigEndianBinaryWriter(_buffer);
        UdpTrackerEncoder.WriteAnnounce(writer, _connectionId!.Value, transactionId, request);
        await _client.SendAsync(_buffer.AsMemory(0, writer.Position), cancellationToken);
        return transactionId;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        var response = await ReceiveAsync(() => SendConnectAsync(cancellationToken), cancellationToken);
        _connectionId = UdpTrackerDecoder.ReadConnectionId(response);
    }

    private async Task<int> SendConnectAsync(CancellationToken cancellationToken = default)
    {
        int transactionId = _random.Next();
        var writer = new BigEndianBinaryWriter(_buffer);
        UdpTrackerEncoder.WriteConnect(writer, transactionId);
        await _client.SendAsync(_buffer.AsMemory(0, writer.Position), cancellationToken);
        return transactionId;
    }

    private async Task<byte[]> ReceiveAsync(Func<Task<int>> send, CancellationToken cancellationToken = default)
    {
        int transactionId = await send();
        if (_receivedMessages.TryGetValue(transactionId, out var message))
        {
            _receivedMessages.Remove(transactionId);
            return message;
        }
        int resends = 0;
        while (true)
        {
            Task<UdpReceiveResult> receiveTask = _client.ReceiveAsync(cancellationToken).AsTask();
            var ready = await Task.WhenAny(receiveTask, Task.Delay(15 * (1 << resends) * 1000, cancellationToken));
            if (ready != receiveTask)
            {
                resends++;
                transactionId = await send();
                continue;
            }
            var receive = await receiveTask;
            var header = UdpTrackerDecoder.DecodeHeader(new(receive.Buffer));
            if (header.Action == 3)
            {
                var errorMessage = UdpTrackerDecoder.ReadErrorMessage(receive.Buffer);
                throw new TrackerException(errorMessage);
            }
            if (header.TransactionId == transactionId)
            {
                return receive.Buffer;
            }
            else
            {
                _receivedMessages[header.TransactionId] = receive.Buffer;
            }
        }
    }
}
