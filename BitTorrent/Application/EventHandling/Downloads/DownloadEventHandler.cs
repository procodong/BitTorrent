using System.Collections;
using System.IO.Pipelines;
using System.Threading.Channels;
using BencodeNET.IO;
using BencodeNET.Torrents;
using BitTorrentClient.Application.EventListening.Downloads;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Protocol.Networking.PeerWire.Handshakes;

namespace BitTorrentClient.Application.EventHandling.Downloads;

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
        DownloadStorage storage = torrent.Files is not null
            ? DownloadStorageFactory.CreateMultiFileStorage(targetPath, torrent.Files, (int)torrent.PieceSize)
            : DownloadStorageFactory.CreateSingleFileStorage(targetPath, torrent.File, (int)torrent.PieceSize);
        await _downloads.AddDownloadAsync(torrent, storage);
    }

    public async Task RemoveTorrentAsync(ReadOnlyMemory<byte> index)
    {
        await _downloads.RemoveDownloadAsync(index);
    }

    public async Task OnTickAsync(CancellationToken cancellationToken = default)
    {
        var updates = _downloads.GetUpdates();
        await _updateSender.WriteAsync(updates, cancellationToken);
    }

    public async Task OnPeerAsync(IHandshakeReceiver<IRespondedHandshakeSender<IBitfieldSender<PeerWireStream>>> peer, CancellationToken cancellationToken = default)
    {
        await _downloads.AddPeerAsync(peer);
    }
}