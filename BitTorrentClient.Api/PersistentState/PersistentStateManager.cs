using System.Text.Json;
using BitTorrentClient.Api.Information;
using BitTorrentClient.Core.Presentation.Torrent;
using BitTorrentClient.Engine.Models.Config;

namespace BitTorrentClient.Api.PersistentState;

public class PersistentStateManager
{
    private readonly ClientFileProvider _fileProvider;

    public PersistentStateManager(string applicationName)
    {
        _fileProvider = new(applicationName);
    }
    public StreamWriter GetLog()
    {
        var log = _fileProvider.GetLogFile();
        log.Seek(0, SeekOrigin.End);
        return new(log);
    }

    public async Task<DownloadModel[]> GetStateAsync()
    {
        await using var stateFile = _fileProvider.GetStateFile();
        if (stateFile.Length == 0)
        {
            await stateFile.WriteAsync("[]"u8.ToArray());
            return [];
        }
        var state = await JsonSerializer.DeserializeAsync<JsonDownloadModel[]>(stateFile);
        if (state is null) return [];
        return state.Select(x => new DownloadModel(x.Data, x.Settings)).ToArray();
    }

    public async Task SaveStateAsync(IEnumerable<DownloadModel> state)
    {
        await using var stateFile = _fileProvider.GetStateFile();
        await JsonSerializer.SerializeAsync(stateFile, state.Select(v => v.Data));
    }
}

class JsonDownloadModel
{
    internal required DownloadData Data { get; set; }
    internal required DownloadSettings Settings { get; set; }
}