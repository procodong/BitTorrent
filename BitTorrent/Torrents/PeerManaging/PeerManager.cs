using BitTorrent.Models.Messages;
using BitTorrent.Models.Peers;
using BitTorrent.Torrents.Downloads;
using BitTorrent.Torrents.Peers;
using BitTorrent.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.PeerManaging;
public class PeerManager<S>(string peerId, Download<S> download)
    where S : Stream
{
    private readonly List<PeerConnector> _peers = [];
    private readonly string _peerId = peerId;
    private readonly Download<S> _download = download;
    private readonly Stopwatch _updateWatch = new();

    public void AddPeer(TcpClient connection)
    {
        var eventChannel = Channel.CreateBounded<Relation>(5);
        var haveChannel = Channel.CreateUnbounded<int>();
        var stats = new PeerStatistics();
        _download.AddPeer(haveChannel.Writer);
        var peer = new PeerConnector(new(stats, new(_download.Torrent.NumberOfPieces)), eventChannel.Writer, new());
        _peers.Add(peer);
        Task.Run(async () =>
        {
            using var peer = await Peer<S>.ConnectAsync(connection, haveChannel.Reader, eventChannel.Reader, _download, stats, _peerId);
            await peer.ListenAsync();
        });
    }

    private async Task UpdatePeerInterests()
    {
        var interestingPeers = new SumPriorityStack<(int Index, long Downloaded)>(v => v.Downloaded, _download.Config.TargetDownload);
        var unchokedPeers = new SumPriorityStack<(int Index, long Uploaded)>(v => v.Uploaded, _download.Config.TargetUpload);

        foreach (var (index, peer) in _peers.Indexed())
        {
            long downloaded = Interlocked.Read(ref peer.Data.Stats.Downloaded);
            long uploaded = Interlocked.Read(ref peer.Data.Stats.Uploaded);

            long timeElapsed = (long)_updateWatch.Elapsed.TotalSeconds;
            _updateWatch.Restart();

            long downloadedChange = downloaded - peer.LastStatistics.Downloaded / timeElapsed;
            long uploadedChange = uploaded - peer.LastStatistics.Uploaded / timeElapsed;

            peer.LastStatistics.Downloaded = downloaded;
            peer.LastStatistics.Uploaded = uploaded;

            interestingPeers.Include((index, downloadedChange));
            unchokedPeers.Include((index, uploadedChange));
        }
        var interestingPeersLookup = new BitArray(_peers.Count);
        var unchokedPeersLookup = new BitArray(_peers.Count);
        w
        foreach (var interesting in interestingPeers)
        {
            interestingPeersLookup[interesting.Index] = true;
        }

        foreach (var unchoke in unchokedPeers)
        {
            unchokedPeersLookup[unchoke.Index] = true;
        }

        foreach (var (index, peer) in _peers.Indexed())
        {
            if (interestingPeersLookup[index])
            {
                await peer.RelationEventWriter.WriteAsync(Relation.Interested);
            }
            else
            {
                await peer.RelationEventWriter.WriteAsync(Relation.NotInterested);
            }

            if (unchokedPeersLookup[index])
            {
                await peer.RelationEventWriter.WriteAsync(Relation.Unchoke);
            }
            else
            {
                await peer.RelationEventWriter.WriteAsync(Relation.Choke);
            }
        }
    }

    private List<int> FindRaresPieces()
    {
        int rareCount = int.Min(_download.Torrent.NumberOfPieces / 100, _download.Config.MaxRarePieceCount);
        var comparer = Comparer<(int Index, int Count)>.Create((v1, v2) => v1.Count - v2.Count);
        var rarestPieceStack = new PriorityStack<(int Index, int Count)>(rareCount, comparer);
        foreach (var piece in PieceCounts())
        {
            rarestPieceStack.Include(piece);
        }
        return rarestPieceStack.Select(v => v.Index).ToList();
    }

    private IEnumerable<(int, int)> PieceCounts()
    {
        for (int i = 0; i < _download.Torrent.NumberOfPieces; i++)
        {
            int count = _peers.Select(peer => peer.Data.OwnedPieces[i] ? 1 : 0).Count();
            yield return (i,count);
        }
    } 
}
