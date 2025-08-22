using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Helpers.DataStructures;
public record struct RentedArray<T>(T[] Buffer, int ExpectedSize);
