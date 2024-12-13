using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Models.Application;
public record class Config(
    int TargetDownload,
    int TargetUpload,
    int TargetUploadSeeding,
    int RequestSize,
    int RequestQueueSize,
    int MaxRarePieceCount,
    int PeerUpdateInterval,
    int MaxRequestSize,
    int KeepAliveInterval,
    int ReceiveTimeout,
    int UiUpdateInterval,
    int PieceSegmentSize,
    int MaxParallelPeers
    );