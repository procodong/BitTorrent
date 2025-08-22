using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Helpers.Extensions;
public static class LoggerExt
{
    public static void LogError(this ILogger logger, string origin, Exception error)
    {
#if DEBUG
        logger.LogError("Error in {origin}: {error}", origin, error);
#else
        logger.LogError("Error in {origin}: {error}", origin, error.Message);
#endif
    }
}
