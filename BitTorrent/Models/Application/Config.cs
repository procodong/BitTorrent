using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Models.Application;
public readonly record struct Config(
    long TargetDownload,
    long TargetUpload,
    int ConcurrentPieceDownloads,
    int RequestSize,
    int RequestQueueSize,
    int MaxRarePieceCount
    );