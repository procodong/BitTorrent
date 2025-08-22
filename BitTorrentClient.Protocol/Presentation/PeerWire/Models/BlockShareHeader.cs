using System.Runtime.InteropServices;

namespace BitTorrentClient.Protocol.Presentation.PeerWire.Models;
[StructLayout(LayoutKind.Sequential)]
public readonly record struct BlockShareHeader(int Index, int Begin);