namespace BitTorrentClient.Models.Peers;
public readonly record struct DataTransferVector(long Download, long Upload)
{
    public static DataTransferVector operator +(DataTransferVector left, DataTransferVector right) => left with { Download = left.Download + right.Download, Upload = left.Upload + right.Upload };
    public static DataTransferVector operator -(DataTransferVector left, DataTransferVector right) => left with { Download = left.Download - right.Download, Upload = left.Upload - right.Upload };
    public static DataTransferVector operator /(DataTransferVector left, double fraq) => left with { Download = (long)(left.Download / fraq), Upload = (long)(left.Upload / fraq) };
}