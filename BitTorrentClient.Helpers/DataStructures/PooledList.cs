using System.Buffers;

namespace BitTorrentClient.Helpers.DataStructures;

public class PooledList<T>
{
    private IMemoryOwner<T> _buffer;
    private int _expectedCapacity;
    private int _length;

    public PooledList(int size)
    {
        _expectedCapacity = int.Max(size, 8);
        _buffer = MemoryPool<T>.Shared.Rent(size);
    }

    public PooledList() : this(-1)
    {
        
    }
    
    public int Length => _length;

    public void Add(T item)
    {
        var buffer = _buffer.Memory;
        if (_length == buffer.Length)
        {
            _expectedCapacity = buffer.Length * 2;
            var newBuffer = MemoryPool<T>.Shared.Rent(_expectedCapacity);
            buffer[.._length].CopyTo(newBuffer.Memory);
            _buffer.Dispose();
            _buffer = newBuffer;
        }
        buffer.Span[_length++] = item;
    }

    public IMemoryOwner<T> Take()
    {
        var old = new SlicedMemoryOwner<T>(_buffer, 0.._length);
        int expectedCapacity = _length < _expectedCapacity ? _expectedCapacity : _buffer.Memory.Length;
        _length = 0;
        _buffer = MemoryPool<T>.Shared.Rent(expectedCapacity);
        return old;
    }
}