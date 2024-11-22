using BitTorrent.Models.Peers;
using BitTorrent.Models.Tracker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Models.Trackers;
public record struct TrackerUpdate(
    byte[] InfoHash,
    string ClientId,
    DataTransferVector DataTransfer,
    long Left,
    TrackerEvent? TrackerEvent,
    string TrackerUrl,
    int DownloadId
    );