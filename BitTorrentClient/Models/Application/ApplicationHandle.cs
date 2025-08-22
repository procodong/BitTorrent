using BitTorrentClient.Application.Infrastructure.Interfaces;

namespace BitTorrentClient.Models.Application;

public readonly record struct ApplicationHandle(Task CompletionTask, IDownloadService DownloadService);