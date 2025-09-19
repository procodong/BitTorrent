using System.Collections;

namespace BitTorrentClient.Helpers.Utility;

public sealed class DefaultValueEnumerator<T> : IEnumerator<T?>
{
    public static readonly DefaultValueEnumerator<T> Instance = new();

    public T? Current => default;

    object IEnumerator.Current => null!;

    public void Dispose()
    {
    }

    public bool MoveNext() => true;

    public void Reset()
    {
    }

}