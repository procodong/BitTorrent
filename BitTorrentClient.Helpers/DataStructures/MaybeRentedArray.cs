using System.Buffers;

namespace BitTorrentClient.Helpers.DataStructures;

public readonly struct MaybeRentedArray<T> : IDisposable
{
    public T[] Buffer { get; }
    public int Size { get; }
    private bool Rented { get; }
    
    public ArraySegment<T> Data => new(Buffer, 0, Size);

    public MaybeRentedArray(int size, bool rented = true)
    {
        Buffer = rented ? ArrayPool<T>.Shared.Rent(size) : new T[size];
        Size = size;
        Rented = rented;
    }

    public MaybeRentedArray(T[] buffer, int? size = null, bool rented = false)
    {
        Buffer = buffer;
        Rented = rented;
        Size = size ?? buffer.Length;
    }
    
    public void Dispose()
    {
        if (Rented)
        {
            ArrayPool<T>.Shared.Return(Buffer);
        }
    }
}