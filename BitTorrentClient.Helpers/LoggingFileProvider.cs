namespace BitTorrentClient.Helpers;

public static class LoggingFileProvider
{
    public static StreamWriter GetLogFile(string applicationName)
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), applicationName, "logs");
        var dir = Directory.CreateDirectory(logPath);
        var logFile = File.Open(Path.Combine(logPath, "error.log"), new FileStreamOptions
        {
            Mode = FileMode.OpenOrCreate,
            Access = FileAccess.ReadWrite,
            Options = FileOptions.Asynchronous
        });
        logFile.Seek(0, SeekOrigin.End);
        return new(logFile);
    }
}