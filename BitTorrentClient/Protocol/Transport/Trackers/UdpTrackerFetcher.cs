using System.Buffers;
using System.Net.Sockets;
using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Models.Trackers;
using BitTorrentClient.Protocol.Presentation.UdpTracker;
using BitTorrentClient.Protocol.Transport.PeerWire.Connecting.Networking;
using BitTorrentClient.Protocol.Transport.Trackers.Exceptions;

namespace BitTorrentClient.Protocol.Transport.Trackers;
public class UdpTrackerFetcher : ITrackerFetcher, IDisposable
{
    private readonly UdpClient _client;
    private readonly Random _random;
    private readonly ArrayBufferWriter<byte> _buffer;
    private readonly int _port;
    private readonly int _peerBufferSize;
    private long _connectionId;

    public UdpTrackerFetcher(UdpClient client, int port, int peerBufferSize)
    {
        _client = client;
        _random = new();
        _buffer = new(1 << 7);
        _port = port;
        _peerBufferSize = peerBufferSize;
    }

    public async Task<TrackerResponse> FetchAsync(TrackerUpdate update, CancellationToken cancellationToken = default)
    {
        var request = new TrackerRequest(update.InfoHash, update.ClientId, _port, update.DataTransfer.Upload, update.DataTransfer.Download, update.Left, update.TrackerEvent);
        var response = await ReceiveAsync(() => SendAnnounceAsync(request, cancellationToken), cancellationToken);
        var data = UdpTrackerDecoder.ReadAnnounceResponse(new(response));
        return new(data.Interval, default, data.Complete, data.Incomplete, data.Peers.Select(addr => new TcpPeerConnector(addr, _peerBufferSize)), default);
    }

    private async Task<int> SendAnnounceAsync(TrackerRequest request, CancellationToken cancellationToken = default)
    {
        int transactionId = _random.Next();
        var writer = new BigEndianBinaryWriter(_buffer);
        UdpTrackerEncoder.WriteAnnounce(writer, _connectionId, transactionId, request);
        await _client.SendAsync(_buffer.WrittenMemory, cancellationToken);
        _buffer.ResetWrittenCount();
        return transactionId;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        var response = await ReceiveAsync(() => SendConnectAsync(cancellationToken), cancellationToken);
        var reader = new BigEndianBinaryReader(response);
        _connectionId = reader.ReadInt64();
    }

    private async Task<int> SendConnectAsync(CancellationToken cancellationToken = default)
    {
        int transactionId = _random.Next();
        var writer = new BigEndianBinaryWriter(_buffer);
        UdpTrackerEncoder.WriteConnect(writer, transactionId);
        await _client.SendAsync(_buffer.WrittenMemory, cancellationToken);
        _buffer.ResetWrittenCount();
        return transactionId;
    }

    private async Task<IBufferReader> ReceiveAsync(Func<Task<int>> send, CancellationToken cancellationToken = default)
    {
        int transactionId = await send();
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
            var cursor = new BufferCursor(receive.Buffer, receive.Buffer.Length);
            var reader = new BigEndianBinaryReader(cursor);
            var header = UdpTrackerDecoder.ReadHeader(reader);
            if (header.Action == 3)
            {
                var errorMessage = reader.ReadString(cursor.RemainingInitializedBytes);
                throw new TrackerException(errorMessage);
            }
            else if (header.TransactionId == transactionId)
            {
                return cursor;
            }
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
