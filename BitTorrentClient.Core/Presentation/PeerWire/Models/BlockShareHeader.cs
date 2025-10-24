using System.Runtime.InteropServices;

namespace BitTorrentClient.Core.Presentation.PeerWire.Models;
[StructLayout(LayoutKind.Sequential)]
public readonly record struct BlockShareHeader(int Index, int Begin);