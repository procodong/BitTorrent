using BitTorrent.Models.Application;
using BitTorrent.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Application.Ui;
public class CliHandler : IUiHandler
{
    private (int Left, int Top) _displayPosition = Console.GetCursorPosition();
    private readonly StringBuilder _buffer = new();
    public void Update(IEnumerable<DownloadUpdate> updates)
    {
        var (left, top) = Console.GetCursorPosition();
        Console.SetCursorPosition(_displayPosition.Left, _displayPosition.Top);
        foreach (var (index, update) in updates.Indexed())
        {
            int downloadedBarCount = unchecked((int)((double)update.Transfered.Download / update.Size * 10));
            _buffer.Append(index);
            _buffer.Append(". ");
            for (int i = 0; i < downloadedBarCount; i++)
            {
                _buffer.Append('█');
            }
            int notDownloadedBarCount = 10 - downloadedBarCount;
            for (int i = 0; i < notDownloadedBarCount; i++)
            {
                _buffer.Append('░');
            }
            _buffer.Append(' ');
            _buffer.Append(update.DownloadName);
            _buffer.Append(" download per second: ");
            _buffer.Append(update.TransferRate.Download);
            _buffer.Append(" upload per second: ");
            _buffer.Append(update.TransferRate.Upload);
            _buffer.Append(" downloaded: ");
            _buffer.Append(update.Transfered.Download);
            _buffer.Append(" uploaded: ");
            _buffer.Append(update.Transfered.Upload);
            _buffer.AppendLine();
        }
        Console.Write(_buffer.ToString());
        _buffer.Clear();
        Console.SetCursorPosition(left, top);
    }
}
