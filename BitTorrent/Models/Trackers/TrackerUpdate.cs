using BitTorrentClient.Models.Peers;
using BitTorrentClient.Models.Trackers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Models.Trackers;
public record struct TrackerUpdate(
    byte[] InfoHash,
    string ClientId,
    DataTransferVector DataTransfer,
    long Left,
    TrackerEvent TrackerEvent
    );