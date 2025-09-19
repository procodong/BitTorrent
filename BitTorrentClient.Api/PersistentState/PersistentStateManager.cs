using System.Text.Json;
using BitTorrentClient.Api.Information;
using BitTorrentClient.Engine.Models;
using BitTorrentClient.Protocol.Presentation.Torrent;

namespace BitTorrentClient.Api.PersistentState;

public class PersistentStateManager
{
    private readonly ClientFileProvider _fileProvider;

    public PersistentStateManager(string applicationName)
    {
        _fileProvider = new(applicationName);
    }

    public async Task<ConfigBuilder> GetConfigAsync()
    {
        await using var configFile = _fileProvider.GetConfigFile();
        if (configFile.Length == 0) return new();
        var config = await JsonSerializer.DeserializeAsync<ConfigBuilder>(configFile);
        return config ?? new();
    }

    public async Task SaveConfigAsync(Config config)
    {
        await using var configFile = _fileProvider.GetConfigFile();
        await JsonSerializer.SerializeAsync(configFile, config);
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
        var state = await JsonSerializer.DeserializeAsync<DownloadData[]>(stateFile);
        if (state is null) return [];
        return state.Select(x => new DownloadModel(x)).ToArray();
    }

    public async Task SaveStateAsync(IEnumerable<DownloadModel> state)
    {
        await using var stateFile = _fileProvider.GetStateFile();
        await JsonSerializer.SerializeAsync(stateFile, state.Select(v => v.Data));
    } 
}