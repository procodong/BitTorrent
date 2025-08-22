using System.Diagnostics.CodeAnalysis;

namespace BitTorrentClient.Engine.Models.Downloads;

public readonly struct DownloadId
{
    public ReadOnlyMemory<byte> Raw { get; }

    public DownloadId(ReadOnlyMemory<byte> id)
    {
        Raw = id;
    }
    
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var b in Raw.Span)
        {
            hash.Add(b);
        }
        return hash.ToHashCode();
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is DownloadId id)
        {
            return Raw.Span.SequenceEqual(id.Raw.Span);
        }
        return false;
    }
}