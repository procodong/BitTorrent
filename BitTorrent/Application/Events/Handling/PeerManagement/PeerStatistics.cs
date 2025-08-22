using BitTorrentClient.Models.Peers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.Events.EventHandling.PeerManagement;
public readonly record struct PeerStatistics(DataTransferVector DataTransferPerSecond, PeerRelation PeerRelation, PeerRelation ClientRelation);