using BencodeNET.Objects;
using BencodeNET.Parsing;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Models.Trackers;
using BitTorrentClient.Torrents.Trackers.Errors;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace BitTorrentClient.Torrents.Trackers;
public class HttpTrackerFetcher : ITrackerFetcher
{
    private readonly HttpClient _httpClient;
    public TrackerResponse? InitialResponse;
    private readonly string _url;
    private readonly int _listenPort;

    public HttpTrackerFetcher(HttpClient httpClient, string url, int listenPort)
    {
        _httpClient = httpClient;
        _listenPort = listenPort;
        _url = url;
    }

    private static string DisplayEvent(TrackerEvent trackerEvent)
    {
        return trackerEvent switch
        {
            TrackerEvent.Started => "started",
            TrackerEvent.Stopped => "stopped",
            TrackerEvent.Completed => "completed",
            _ => throw new ArgumentException("Invalid tracker event")
        };
    }

    private TrackerRequest GetRequest(TrackerUpdate update) =>
        new(update.InfoHash, update.ClientId, _listenPort, update.DataTransfer.Upload, update.DataTransfer.Download, update.Left, update.TrackerEvent);

    public async Task<TrackerResponse> FetchAsync(TrackerUpdate update, CancellationToken cancellationToken = default)
    {
        if (InitialResponse is not null)
        {
            var respone = InitialResponse;
            InitialResponse = null;
            return respone;
        }
        var request = GetRequest(update);
        var builder = new UriBuilder(_url)
        {
            Port = -1
        };
        var query = HttpUtility.ParseQueryString(builder.Query);
        query["info_hash"] = Uri.EscapeDataString(Convert.ToBase64String(request.InfoHash));
        query["peer_id"] = request.ClientId;
        query["port"] = request.Port.ToString();
        query["uploaded"] = request.Uploaded.ToString();
        query["downloaded"] = request.Downloaded.ToString();
        query["left"] = request.Left.ToString();
        query["compact"] = "0";
        query["no_peer_id"] = "0";
        if (request.TrackerEvent != TrackerEvent.None)
        {
            query["event"] = DisplayEvent(request.TrackerEvent);
        }
        builder.Query = query.ToString();

        var response = await _httpClient.GetAsync(builder.ToString(), cancellationToken);
        var parser = new BencodeParser();
        var content = await parser.ParseAsync<BDictionary>(response.Content.ReadAsStream(cancellationToken), cancellationToken: cancellationToken);
        var error = content.Get<BString?>("failure reason");
        if (error is not null)
        {
            throw new TrackerException(error.ToString());
        }
        if (!response.IsSuccessStatusCode)
        {
            throw new TrackerHttpException((int)response.StatusCode);
        }
        return new(
            Interval: content.Get<BNumber>("interval"),
            MinInterval: content.Get<BNumber>("min interval"),
            Complete: content.Get<BNumber>("complete"),
            Incomplete: content.Get<BNumber>("incomplete"),
            Peers: content.Get<BList<BDictionary>>("peers").Value
            .Select(obj => (BDictionary)obj)
            .Select(value => new PeerAddress(
                Ip: IPAddress.Parse(value.Get<BString>("ip").ToString()),
                Port: value.Get<BNumber>("port")
                )).ToList(),
            Warning: content.Get<BString?>("warning message")?.ToString()
            );
    }
}
