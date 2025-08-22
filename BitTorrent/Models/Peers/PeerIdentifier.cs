using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace BitTorrentClient.Models.Peers;

public readonly record struct PeerIdentifier(string ClientId, Version Version);

[StructLayout(LayoutKind.Sequential)]
public readonly record struct Version(char Major, char Minor, char Patch, char Revision)
{
    public override readonly string ToString() => new(MemoryMarshal.CreateSpan(ref Unsafe.As<Version, char>(ref Unsafe.AsRef(in this)), 4));
}
