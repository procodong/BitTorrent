using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Helpers.Streams;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Reading;
public class PeerWireReader : IDisposable, IAsyncDisposable, IPeerWireReader
{
    private readonly BufferedMessageStream _stream;

    public PeerWireReader(BufferedMessageStream stream)
    {
        _stream = stream;
    }

    public async Task<IMessageFrameReader> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        var message = await _stream.ReadMessageAsync(cancellationToken);
        await message.EnsureReadAtLeastAsync(1, cancellationToken);
        var reader = new BigEndianBinaryReader(message);
        var type = (MessageType)reader.ReadByte();
        var frame = new MessageFrameReader(message, type);
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
