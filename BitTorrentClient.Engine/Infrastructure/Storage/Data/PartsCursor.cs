namespace BitTorrentClient.Engine.Infrastructure.Storage.Data;

public class PartCursor
{
    private readonly IEnumerator<StreamPart> _parts;
    private int _filePosition;

    public PartCursor(IEnumerable<StreamPart> parts)
    {
        _parts = parts.GetEnumerator();
        _parts.MoveNext();
    }

    public int RemainingInPart => _parts.Current.Length - _filePosition;
    
    private bool Next()
    {
        _filePosition = 0;
        return _parts.MoveNext();
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
        part = _parts.Current;
        return true;
    }

    public void Advance(int count)
    {
        _filePosition += count;
    }
}