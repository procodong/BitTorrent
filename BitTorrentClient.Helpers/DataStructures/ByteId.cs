using System.Diagnostics.CodeAnalysis;

namespace BitTorrentClient.Helpers.DataStructures;

public readonly struct ByteId : IEquatable<ByteId>
{
    private ReadOnlyMemory<byte> Raw { get; }

    public ByteId(ReadOnlyMemory<byte> id)
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

    public bool Equals(ByteId other)
    {
        return Raw.Span.SequenceEqual(other.Raw.Span);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is ByteId id)
        {
            return Raw.Span.SequenceEqual(id.Raw.Span);
        }
        return false;
    }

    public static bool operator ==(ByteId left, ByteId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ByteId left, ByteId right)
    {
        return !(left == right);
    }
}