using System.Buffers;

namespace BitTorrentClient.Helpers.DataStructures;
public class SlicedMemoryOwner<T> : IMemoryOwner<T>
{
    private readonly IMemoryOwner<T> _inner;
    private readonly Range _range;

    public SlicedMemoryOwner(IMemoryOwner<T> inner, Range range)
    {
        _inner = inner;
        _range = range;
    }

    public Memory<T> Memory => _inner.Memory[_range];

    public void Dispose()
    {
        _inner.Dispose();
    }
}
