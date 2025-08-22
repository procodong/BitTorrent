using System.IO.Pipelines;
using System.Threading.Channels;
using BencodeNET.IO;
using BencodeNET.Torrents;
using BitTorrentClient.Application.Events.Listening.Downloads;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Application.Events.Handling.Downloads;

public class DownloadEventHandler : IDownloadEventHandler
{
    private readonly IDownloadCollection _downloads;
    private readonly ChannelWriter<IEnumerable<DownloadUpdate>> _updateSender;

    public DownloadEventHandler(IDownloadCollection downloads, ChannelWriter<IEnumerable<DownloadUpdate>> updateSender)
    {
        _downloads = downloads;
        _updateSender = updateSender;
    }

    public async Task AddTorrentAsync(string torrentPath, string targetPath)
    {
        await using var file = File.Open(torrentPath, new FileStreamOptions()
        {
            Options = FileOptions.Asynchronous,
        });
        var parser = new TorrentParser();
        var stream = new PipeBencodeReader(PipeReader.Create(file));
        var torrent = await parser.ParseAsync(stream);
        FileStreamProvider storage = torrent.Files is not null
            ? DownloadStorageFactory.CreateMultiFileStorage(targetPath, torrent.Files)
            : DownloadStorageFactory.CreateSingleFileStorage(targetPath, torrent.File);
        _ = _downloads.AddDownloadAsync(torrent, storage);
    }

    public Task RemoveTorrentAsync(ReadOnlyMemory<byte> identifier)
    {
        _ = _downloads.RemoveDownloadAsync(identifier);
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