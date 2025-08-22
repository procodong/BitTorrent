using System.IO.Pipelines;
using System.Threading.Channels;
using BencodeNET.IO;
using BencodeNET.Torrents;
using BitTorrentClient.Application.Events.Listening.Downloads;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Application.Events.Handling.Downloads;

public class DownloadEventHandler : IDownloadEventHandler
{
    private readonly IDownloadCollection _downloads;
    private readonly ChannelWriter<IEnumerable<DownloadUpdate>> _updateSender;
    private readonly ILogger _logger;

    public DownloadEventHandler(IDownloadCollection downloads, ChannelWriter<IEnumerable<DownloadUpdate>> updateSender, ILogger logger)
    {
        _downloads = downloads;
        _updateSender = updateSender;
        _logger = logger;
    }

    public async Task AddTorrentAsync(string torrentPath, string targetPath, string? name)
    {
        await using var file = File.Open(torrentPath, new FileStreamOptions()
        {
            Options = FileOptions.Asynchronous,
        });
        var parser = new TorrentParser();
        var stream = new PipeBencodeReader(PipeReader.Create(file));
        var torrent = await parser.ParseAsync(stream);
        StorageStream storage = torrent.Files is not null
            ? DownloadStorageFactory.CreateMultiFileStorage(targetPath, torrent.Files)
            : DownloadStorageFactory.CreateSingleFileStorage(targetPath, torrent.File);
        _ = AddDownloadAsync(torrent, storage, name);
    }

    private async Task AddDownloadAsync(Torrent torrent, StorageStream storage, string? name)
    {
        try
        {
            await _downloads.AddDownloadAsync(torrent, storage, name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Adding download");
        }
    }

    public Task RemoveTorrentAsync(string name)
    {
        _ = _downloads.RemoveDownloadAsync(v => v.Name == name);
        return Task.CompletedTask;
    }
    public Task RemoveTorrentAsync(int index)
    {
        _ = _downloads.RemoveDownloadAsync(v => v.DownloadIndex == index);
        return Task.CompletedTask;
    }

    public async Task OnTickAsync(CancellationToken cancellationToken = default)
    {
        var updates = _downloads.GetUpdates();
        await _updateSender.WriteAsync(updates, cancellationToken);
    }

    public Task OnPeerAsync(IHandshakeReceiver<IRespondedHandshakeSender<IBitfieldSender<PeerWireStream>>> peer, CancellationToken cancellationToken = default)
    {
        _ = _downloads.AddPeerAsync(peer);
        return Task.CompletedTask;
    }
}