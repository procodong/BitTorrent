using System.Text.Json;
using BitTorrentClient.Models.Application;

namespace BitTorrentClient.Application.State;

public class PersistenStateManager
{
    private readonly ApplicationFileProvider _fileProvider;

    public PersistenStateManager(string applicationName)
    {
        _fileProvider = new(applicationName);
    }

    public async Task<Config> GetConfigAsync()
    {
        await using var configFile = _fileProvider.GetConfigFile();
        var config = await JsonSerializer.DeserializeAsync<Config>(configFile);
        return config ?? Config.Default;
    }

    public async Task SaveConfigAsync(Config config)
    {
        await using var configFile = _fileProvider.GetConfigFile();
        await JsonSerializer.SerializeAsync(configFile, config);
    }

    public StreamWriter GetLog()
    {
        return new(_fileProvider.GetLogFile());
    }

    public async Task<DownloadData[]> GetStateAsync()
    {
        await using var stateFile = _fileProvider.GetStateFile();
        var state = await JsonSerializer.DeserializeAsync<DownloadData[]>(stateFile);
        return state ?? [];
    }

    public async Task SaveStateAsync(DownloadData[] state)
    {
        await using var stateFile = _fileProvider.GetStateFile();
        await JsonSerializer.SerializeAsync(stateFile, state);
    } 
}