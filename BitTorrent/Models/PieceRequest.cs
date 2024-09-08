using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Models;
public readonly record struct PieceRequest(int Index, int Begin, int Length);