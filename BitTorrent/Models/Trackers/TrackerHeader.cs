using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Models.Trackers;
public readonly record struct TrackerHeader(int Action, int TransactionId);