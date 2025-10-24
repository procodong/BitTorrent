using BitTorrentClient.Core.Presentation.PeerWire.Models;
using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Helpers.Streams;

namespace BitTorrentClient.Core.Transport.PeerWire.Reading;
public sealed class PeerWireReader : IPeerWireReader
{
    private readonly BufferedMessageStream _stream;
    private readonly SemaphoreSlim _readLock;

    public PeerWireReader(BufferedMessageStream stream)
    {
        _readLock = new(1, 1);
        _stream = stream;
    }

    public async Task<IMessageFrameReader> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        await _readLock.WaitAsync(cancellationToken);
        var message = await _stream.ReadMessageAsync(cancellationToken);
        await message.EnsureReadAtLeastAsync(1, cancellationToken);
        var reader = new BigEndianBinaryReader(message);
        var type = (MessageType)reader.ReadByte();
        var frame = new MessageFrameReader(message, type, _readLock);
        await message.EnsureReadAtLeastAsync(int.Min(message.AvailableBuffer, message.Remaining), cancellationToken);
        return frame;
    }
    public void Dispose()
    {
        _stream.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _stream.DisposeAsync();
    }
}
