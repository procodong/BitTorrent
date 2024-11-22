using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Files.DownloadFiles;
public readonly record struct StreamPart(StreamData StreamData, int Length, long Position);