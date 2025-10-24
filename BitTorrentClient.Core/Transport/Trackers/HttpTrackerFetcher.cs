using BencodeNET.Objects;
using BencodeNET.Parsing;
using System.Net;
using System.Text;
using System.Web;
using BitTorrentClient.Core.Presentation.UdpTracker.Models;
using BitTorrentClient.Core.Transport.PeerWire.Connecting;
using BitTorrentClient.Core.Transport.Trackers.Exceptions;
using BitTorrentClient.Core.Transport.Trackers.Interface;

namespace BitTorrentClient.Core.Transport.Trackers;
public sealed class HttpTrackerFetcher : ITrackerFetcher
{
    private readonly HttpClient _httpClient;
    private readonly int _peerBufferSize;
    private readonly Uri _url;
    private readonly int _listenPort;
    
    public TrackerResponse? InitialResponse { get; set; }

    public HttpTrackerFetcher(HttpClient httpClient, Uri url, int listenPort, int peerBufferSize)
    {
        _httpClient = httpClient;
        _listenPort = listenPort;
        _url = url;
        _peerBufferSize = peerBufferSize;
    }

    public async Task<TrackerResponse> FetchAsync(TrackerUpdate update, CancellationToken cancellationToken = default)
    {
        if (InitialResponse is not null)
        {
            var res = InitialResponse;
            InitialResponse = null;
            return res;
        }
        
        var request = GetRequest(update);
        var builder = new UriBuilder(_url)
        {
            Port = -1
        };
        var query = HttpUtility.ParseQueryString(builder.Query);
        query["info_hash"] = Uri.EscapeDataString(Convert.ToBase64String(request.InfoHash.Span));
        query["peer_id"] = Encoding.ASCII.GetString(request.ClientId.Span);
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
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var content = await parser.ParseAsync<BDictionary>(stream, cancellationToken: cancellationToken);
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
            .Select(value => new IPEndPoint(
                IPAddress.Parse(value.Get<BString>("ip").ToString()),
                value.Get<BNumber>("port")
                ))
            .Select(addr => new TcpPeerConnector(
                addr,
                _peerBufferSize
                ))
            .ToArray(),
            Warning: content.Get<BString?>("warning message")?.ToString()
            );
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

    public void Dispose()
    {
    }
}
