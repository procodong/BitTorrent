using BitTorrentClient.Helpers;
using BitTorrentClient.Models.Application;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace BitTorrentClient.Tui.Interface.Output;

public class UiHandler
{
    private readonly Table _ui;
    private readonly Table _downloadsTable;
    private readonly Table _messages;
    private readonly Dictionary<ReadOnlyMemory<byte>, int> _downloads;
    private readonly int _messageLimit;

    private UiHandler(Table ui, Table downloadsTable, Table messagesTable, int messageLimit)
    {
        _ui = ui;
        _downloadsTable = downloadsTable;
        _messages = messagesTable;
        _downloads = new(new MemoryComparer<byte>());
        _messageLimit = messageLimit;
    }

    public static UiHandler Create(Table table, int messageLimit)
    {
        var downloadsTable = new Table();
        downloadsTable.AddColumns("Name", "Size", "State", "Progress", "Downloaded", "Uploaded");

        var messagesTable = new Table();
        messagesTable.AddColumns("Type", "Time", "Message");
        table.AddColumn("BitTorrent");
        table.AddRow(downloadsTable);
        table.AddRow(messagesTable);

        var handler = new UiHandler(table, downloadsTable, messagesTable, messageLimit);
        handler.Update();
        return handler;
    }

    public void Update(IEnumerable<DownloadUpdate> updates)
    {
        bool any = false;
        foreach (var update in updates)
        {
            any = true;
            var progress = update.Transfer.Download / update.Size * 100;
            if (_downloads.TryGetValue(update.Identifier, out var downloadRow))
            {
                _downloadsTable.UpdateCell(downloadRow, 2, update.ExecutionState.ToString());
                _downloadsTable.UpdateCell(downloadRow, 3, progress.ToString());
                _downloadsTable.UpdateCell(downloadRow, 4, update.Transfer.Download.ToString());
                _downloadsTable.UpdateCell(downloadRow, 5, update.Transfer.Upload.ToString());
            }
            else
            {
                int index = _downloadsTable.Rows.Count;
                _downloadsTable.AddRow(update.DownloadName, update.Size.ToString(), update.ExecutionState.ToString(), $"{progress}%", update.Transfer.Download.ToString(), update.Transfer.Upload.ToString());
                _downloads.Add(update.Identifier, index);
            }
        }
        if (any)
        {
            Update();
        }
    }

    public void AddMessage(LogLevel level, string message)
    {
        var time = DateTime.Now.ToString("ddd HH:mm:ss");
        _messages.Rows.Insert(0, [new Text(level.ToString()), new Text(time), new Text(message)]);
        if (_messages.Rows.Count == _messageLimit)
        {
            _messages.Rows.RemoveAt(_messages.Rows.Count - 1);
        }
        Update();
    }

    private void Update()
    {
        
        AnsiConsole.Clear();
        AnsiConsole.Write(_ui);
        AnsiConsole.WriteLine();
    }
}