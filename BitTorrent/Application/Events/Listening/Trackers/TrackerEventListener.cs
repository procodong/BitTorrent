using BitTorrentClient.Models.Trackers;
using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Application.Events.Listening.Trackers;
public class TrackerEventListener
{
    private readonly TcpListener _listener;
    private readonly ITrackerListeningEventHandler _handler;
    private readonly ILogger _logger;

    public TrackerEventListener(ITrackerListeningEventHandler handler, TcpListener listener, ILogger logger)
    {
        _listener = listener;
        _logger = logger;
        _handler = handler;
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
                TcpClient client = await clientTask;
                try
                {
                    await _handler.OnNewPeerAsync(client);
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
                PeerReceivingSubscribe download = await newDownloadTask;
                try
                {
                    await _handler.OnPeerReceivingSubscription(download);
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
}
