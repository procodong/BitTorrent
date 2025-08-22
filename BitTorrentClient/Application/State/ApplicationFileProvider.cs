namespace BitTorrentClient.Application.State;

internal class ApplicationFileProvider
{
    private readonly string _name;

    public ApplicationFileProvider(string name)
    {
        _name = name;
    }

    public FileStream GetLogFile()
    {
        return GetFile("error.log");
    }

    public FileStream GetConfigFile()
    {
        return GetFile("config.json");
    }

    public FileStream GetStateFile()
    {
        return GetFile("state.json");
    }

    private FileStream GetFile(string fileName)
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _name);
        Directory.CreateDirectory(logPath);
        var logFile = File.Open(Path.Combine(logPath, fileName), new FileStreamOptions
        {
            Mode = FileMode.OpenOrCreate,
            Access = FileAccess.ReadWrite,
            Options = FileOptions.Asynchronous
        });
        logFile.Seek(0, SeekOrigin.End);
        return logFile;
    }
}