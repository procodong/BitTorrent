using BencodeNET.Torrents;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Models.Peers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.Infrastructure.Downloads;
public record class DownloadState(Torrent Torrent, Config Config, LazyBitArray DownloadedPieces, List<ChannelWriter<int>> Peers, DataTransferCounter DataTransfer, string ClientId);