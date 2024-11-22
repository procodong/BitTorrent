using BencodeNET.Objects;
using BencodeNET.Parsing;
using BitTorrent.Errors;
using BitTorrent.Models.Peers;
using BitTorrent.Models.Tracker;
using BitTorrent.Torrents.Trackers.Errors;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace BitTorrent.Torrents.Trackers;
public static class TrackerFetcher
{
    public async static Task<TrackerResponse> Fetch(HttpClient client, string url, TrackerRequest request)
    {
        var builder = new UriBuilder(url)
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
        if (request.TrackerEvent.HasValue)
        {
            query["event"] = DisplayEvent(request.TrackerEvent.Value);
        }
        builder.Query = query.ToString();

        var response = await client.GetAsync(builder.ToString());
        var parser = new BencodeParser();
        var content = await parser.ParseAsync<BDictionary>(response.Content.ReadAsStream());
        var error = content.Get<BString?>("failure reason");
        if (error is not null)
        {
            throw new TrackerException(error.ToString());
        }
        if (!response.IsSuccessStatusCode)
        {
            throw new TrackerHttpException((int)response.StatusCode);
        }
        var data = new TrackerResponse(
            Interval: content.Get<BNumber>("interval"),
            MinInterval: content.Get<BNumber>("min interval"),
            TrackerId: content.Get<BString>("tracker id").ToString(),
            Complete: content.Get<BNumber>("complete"),
            Incomplete: content.Get<BNumber>("incomplete"),
            Peers: content.Get<BList<BDictionary>>("peers").Value
            .Select(obj => (BDictionary)obj)
            .Select(value => new PeerAddress(
                Id: value.Get<BString>("peer id").ToString(),
                Ip: IPAddress.Parse(value.Get<BString>("ip").ToString()),
                Port: value.Get<BNumber>("port")
                )).ToList(),
            Warning: content.Get<BString?>("warning message")?.ToString()
            );
        return data;
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
}