using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.Infrastructure.Storage.Data;
public readonly record struct StreamPart(StreamData StreamData, int Length, long Position);