using System;
using System.Buffers;

namespace BitTorrentClient.Helpers.DataStructures;

public readonly struct MaybeRentedArray<T> : IDisposable
{
    public T[] Buffer { get; }
    public int Size { get; }
    public bool Rented { get; }
    
    public ArraySegment<T> Data => new ArraySegment<T>(Buffer, 0, Size);

    public MaybeRentedArray(int size)
    {
        Buffer = ArrayPool<T>.Shared.Rent(size);
        Size = size;
        Rented = true;
    }

    public MaybeRentedArray(T[] buffer)
    {
        Buffer = buffer;
        Size = buffer.Length;
    }
    
    public void Dispose()
    {
        if (Rented)
        {
            ArrayPool<T>.Shared.Return(Buffer);
        }
    }
}