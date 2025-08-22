using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.EventListening.FileWriting;
internal class DataStoringEventListener
{
    private readonly ChannelReader<ReadOnlyMemory<byte>> _bufferReader;
    private readonly ChannelReader<Exception> _exceptionReader;
    private readonly IDataStoringEventHandler _handler;

    public DataStoringEventListener(IDataStoringEventHandler handler, ChannelReader<ReadOnlyMemory<byte>> bufferReader, ChannelReader<Exception> exceptionReader)
    {
        _handler = handler;
        _bufferReader = bufferReader;
        _exceptionReader = exceptionReader;
    }

    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        Task<ReadOnlyMemory<byte>> bufferTask = _bufferReader.ReadAsync(cancellationToken).AsTask();
        Task<Exception> exceptionTask = _exceptionReader.ReadAsync(cancellationToken).AsTask();

        while (true)
        {
            var ready = await Task.WhenAny(bufferTask, exceptionTask);
            if (ready == bufferTask)
            {
                var buffer = await bufferTask;
                await _handler.OnDataAsync(buffer, cancellationToken);
                bufferTask = _bufferReader.ReadAsync(cancellationToken).AsTask();
            }
            else if (ready == exceptionTask)
            {
                var exc = await exceptionTask;
                await _handler.OnExceptionAsync(exc, cancellationToken);
                exceptionTask = _exceptionReader.ReadAsync(cancellationToken).AsTask();
            }
        }
    } 
}
