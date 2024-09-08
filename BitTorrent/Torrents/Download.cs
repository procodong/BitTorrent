
using BencodeNET.Torrents;
using BitTorrent.Files;
using BitTorrent.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents;
public class Download(Torrent torrent, FileManager files)
{
    public readonly Utils.BitArray DownloadedPieces = new(new byte[torrent.NumberOfPieces]);
    public readonly PeerStatistics Statistics = new();
    public readonly Torrent Torrent = torrent;
    public readonly FileManager Files = files;
    public readonly Stack<Range> NotDownloadedPieces = new([0..torrent.NumberOfPieces]);


    public Range AssignPieces(BitArray ownedPieces)
    {
        var lastRange = NotDownloadedPieces.Pop();
        var start = lastRange.Start.Value;
        while (!ownedPieces[start])
        {
            start++;
        }
        int end = start;
        var count = int.Min(start + 10, lastRange.End.Value);
        for (; end <= count; end++)
        {
            if (!ownedPieces[end])
            {
                break;
            }
        }
        if (start != lastRange.Start.Value)
        {
            NotDownloadedPieces.Push(lastRange.Start.Value..start);
        }
        if (end != lastRange.End.Value)
        {
            NotDownloadedPieces.Push(end..lastRange.End.Value);
        }
        return start..end;
    }
}
