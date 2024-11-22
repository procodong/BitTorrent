using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Models.Peers;
public readonly record struct DataTransferVector(long Downloaded, long Uploaded)
{
    public static DataTransferVector operator +(DataTransferVector left, DataTransferVector right) => left with { Downloaded = left.Downloaded + right.Downloaded, Uploaded = left.Uploaded + right.Uploaded };
    public static DataTransferVector operator -(DataTransferVector left, DataTransferVector right) => left with { Downloaded = left.Downloaded - right.Downloaded, Uploaded = left.Uploaded - right.Uploaded };
    public static DataTransferVector operator /(DataTransferVector left, double fraq) => left with { Downloaded = (long)(left.Downloaded / fraq), Uploaded = (long)(left.Uploaded / fraq) };
}