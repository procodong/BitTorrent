namespace BitTorrentClient.Helpers.DataStructures;
public readonly record struct DataTransferVector(long Download, long Upload)
{
    public static DataTransferVector operator +(DataTransferVector left, DataTransferVector right) => new(Download: left.Download + right.Download, Upload: left.Upload + right.Upload);
    public static DataTransferVector operator -(DataTransferVector left, DataTransferVector right) => new(Download: left.Download - right.Download, Upload: left.Upload - right.Upload);
    public static DataTransferVector operator /(DataTransferVector left, double fraq) => new(Download: (long)(left.Download / fraq), Upload: (long)(left.Upload / fraq));
    public static DataTransferVector operator /(DataTransferVector left, DataTransferVector right) => new((long)((double)left.Download / right.Download), (long)((double)left.Upload / right.Upload));
    public static DataTransferVector operator *(DataTransferVector left, DataTransferVector right) => new(left.Download * right.Download, left.Upload * right.Upload);
    
}