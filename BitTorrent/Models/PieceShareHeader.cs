using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Models;
public readonly record struct PieceShareHeader(int Index, int Begin);

public readonly record struct PieceShare(int Index, int Begin, ReadOnlyMemory<byte> Block);