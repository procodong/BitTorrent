using BencodeNET.IO;
using BencodeNET.Torrents;
using BitTorrent.Application.Input.Commands;
using BitTorrent.Application.Ui;
using BitTorrent.Files.DownloadFiles;
using BitTorrent.Models.Application;
using BitTorrent.Torrents.Managing;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrent.Application;
public class ApplicationManager : ICommandContext
{
    private readonly ChannelReader<ICommand> _commandReader;
    private readonly DownloadCollection _downloads;
    private readonly IUiHandler _uiHandler;
    private readonly Config _config;

    public ApplicationManager(ChannelReader<ICommand> commandReader, DownloadCollection downloads, IUiHandler uiHandler, Config config)
    {
        _commandReader = commandReader;
        _downloads = downloads;
        _uiHandler = uiHandler;
        _config = config;
    }

    public async Task ListenAsync()
    {
        Task updateIntervalTask = Task.Delay(_config.UiUpdateInterval);
        Task<ICommand> commandTask = _commandReader.ReadAsync().AsTask();
        while (true)
        {
            var ready = await Task.WhenAny(updateIntervalTask, commandTask);
            if (ready == updateIntervalTask)
            {
                if (_downloads.HasUpdates)
                {
                    var updates = _downloads.GetUpdates();
                    _uiHandler.Update(updates);
                }
                updateIntervalTask = Task.Delay(_config.UiUpdateInterval);
            }
            else if (ready == commandTask)
            {
                var command = await commandTask;
                await command.Run(this);
                commandTask = _commandReader.ReadAsync().AsTask();
            }
        }
    }

    async Task ICommandContext.AddTorrent(string torrentPath, string targetPath)
    {
        var file = File.Open(torrentPath, FileMode.Open);
        var parser = new TorrentParser();
        var stream = new PipeBencodeReader(PipeReader.Create(file));
        var torrent = await parser.ParseAsync(stream);
        var files = DownloadSaveManager.CreateFiles(targetPath, torrent.Files, (int)torrent.PieceSize);
        await _downloads.StartDownload(torrent, files);
    }

    async Task ICommandContext.RemoveTorrent(int index)
    {
        await _downloads.RemoveDownload(index);
    }
}
