using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Models.Application;
public readonly record struct Config(
    ulong MaxDownload,
    ulong MaxUpload,
    int ConcurrentPieceDownloads,
    int RequestSize
    );