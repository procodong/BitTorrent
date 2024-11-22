using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Models.Application;
public readonly record struct Config(
    int TargetDownload,
    int TargetUpload,
    int TargetUploadSeeding,
    int RequestSize,
    int RequestQueueSize,
    int MaxRarePieceCount,
    int PeerUpdateInterval,
    int MaxRequestSize,
    int KeepAliveInterval,
    int ReceivedTimeout,
    int RarePiecesUpdateInterval
    );