namespace BitTorrentClient.Engine.Storage.Data;

public sealed class PartsCursor
{
    private readonly IEnumerator<StreamPart> _parts;
    private long _filePosition;
    private long _fileLength;

    public PartsCursor(IEnumerable<StreamPart> parts)
    {
        _parts = parts.GetEnumerator();
        _parts.MoveNext();
    }

    public long RemainingInPart => _fileLength - _filePosition;
    
    private bool Next()
    {
        var ret = _parts.MoveNext();
        if (!ret) return false;
        var current = _parts.Current;
        _filePosition = current.Position;
        _fileLength = current.Length;
        return true;
    }

    private bool UpdateCurrentFile()
    {
        if (_filePosition >= _parts.Current.Length)
        {
            return Next();
        }
        return true;
    }

    public bool TryGetPart(out StreamPart part)
    {
        if (!UpdateCurrentFile())
        {
            part = default;
            return false;
        }
        part = _parts.Current with {Position = _filePosition};
        return true;
    }

    public void Advance(int count)
    {
        _filePosition += count;
    }
}