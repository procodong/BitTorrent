using BitTorrentClient.Application.Infrastructure.Downloads.Interface;

namespace BitTorrentClient.Models.Application;

public readonly record struct ApplicationHandle(Task CompletionTask, IDownloadService DownloadService);