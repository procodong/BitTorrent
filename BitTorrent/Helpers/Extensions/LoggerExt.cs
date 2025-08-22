using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Utils;
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
