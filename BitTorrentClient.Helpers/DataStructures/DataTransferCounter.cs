namespace BitTorrentClient.Helpers.DataStructures;
public sealed class DataTransferCounter
{
    private long _downloaded;
    private long _uploaded;

    public DataTransferCounter(DataTransferVector vector)
    {
        _downloaded = vector.Download;
        _uploaded = vector.Upload;
    }

    public DataTransferCounter() { }
    
    public long Downloaded => Interlocked.Read(ref _downloaded);
    public long Uploaded => Interlocked.Read(ref _uploaded);

    public DataTransferVector AsVector() => new(Downloaded, Uploaded);

    public DataTransferVector FetchReplace(DataTransferVector vector)
    {
        var downloaded = Interlocked.Exchange(ref _downloaded, vector.Download);
        var uploaded = Interlocked.Exchange(ref _uploaded, vector.Upload);
        return new(downloaded, uploaded);
    }

    public DataTransferVector Fetch()
    {
        return new(
            Interlocked.Read(ref _downloaded),
            Interlocked.Read(ref _uploaded)
            );
    }

    public void AtomicAdd(DataTransferVector vector)
    {
        Interlocked.Add(ref _downloaded, vector.Download);
        Interlocked.Add(ref _uploaded, vector.Upload);
    }
    
    public long AtomicAddDownload(long value) => Interlocked.Add(ref _downloaded, value);
    public long AtomicAddUpload(long value) => Interlocked.Add(ref _uploaded, value);
}
