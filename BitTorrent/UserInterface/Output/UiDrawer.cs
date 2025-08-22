using BitTorrentClient.Models.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.UserInterface.Output;
public class UiDrawer
{
    private (int Left, int Top) _displayPosition = Console.GetCursorPosition();
    private (int Left, int Top) _originalPosition;
    private readonly StringBuilder _buffer = new();

    public void StartDraw()
    {
        _originalPosition = Console.GetCursorPosition();
        Console.SetCursorPosition(_displayPosition.Left, _displayPosition.Top);
    }

    public void Draw(int index, DownloadUpdate update)
    {

        int downloadedBarCount = unchecked((int)((double)update.Transfer.Download / update.Size * 10));
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
        _buffer.Append(update.Transfer.Download);
        _buffer.Append(" uploaded: ");
        _buffer.Append(update.Transfer.Upload);
        _buffer.AppendLine();
    }

    public void EndDraw()
    {
        Console.Write(_buffer.ToString());
        _buffer.Clear();
        Console.SetCursorPosition(_originalPosition.Left, _originalPosition.Top);
    }
}
