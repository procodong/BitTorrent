using BitTorrent.Errors;
using BitTorrent.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrent.Torrents;
public class PeerManager(Download download, string peerId)
{
    private readonly List<(PeerStatistics Stats, ChannelWriter<PeerManagerEvent> Sender)> peers = [];
    private readonly Download download = download;
    private readonly string peerId = peerId;
    

    public void AddPeer(Models.Peer peerData)
    {
        var channel = Channel.CreateUnbounded<PeerManagerEvent>();
        var stats = new PeerStatistics();
        peers.Add((stats, channel.Writer));
        Task.Run(async () =>
        {
            using var peer = await Peer.Connect(peerData, download, channel.Reader, stats);
            await peer.Start(peerId);
        });
    }

    public async Task<(string, TrackerResponse)> FindTracker(int clientPort, TrackerEvent trackerEvent)
    {
        using var httpClient = new HttpClient();
        var request = new TrackerRequest(
            InfoHash: download.Torrent.OriginalInfoHashBytes,
            ClientId: peerId,
            Port: clientPort,
            Uploaded: download.Statistics.Uploaded,
            Downloaded: download.Statistics.Downloaded,
            Left: (ulong)download.Torrent.TotalSize - download.Statistics.Downloaded,
            TrackerEvent: trackerEvent
            );
        foreach (var trackers in download.Torrent.Trackers)
        {
            foreach (var tracker in trackers)
            {
                try
                {
                    var response = await TrackerFetcher.Fetch(httpClient, tracker, request);
                    return (tracker, response);
                } 
                catch (Exception ex)
                {
                    if (ex is TrackerException)
                    {
                        throw;
                    }
                }
            }
        }
        throw new NoValidTrackerException();
    }

    public async Task Listen(Config config)
    {
        
        while (true)
        {
            await Task.Delay(5000);
            if (download.Statistics.Uploaded > config.MaxUpload)
            {

            }

        }
    }
}
