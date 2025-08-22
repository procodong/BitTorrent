using BitTorrentClient.Application.EventListening.Peers;
using BitTorrentClient.BitTorrent.Peers.Errors;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Helpers.Streams;
using BitTorrentClient.Models.Messages;
using BitTorrentClient.Protocol.Presentation.PeerWire;
using System.Reflection.PortableExecutable;

namespace BitTorrentClient.Protocol.Networking.PeerWire;
public class PeerWireReader : IDisposable, IAsyncDisposable
{
    private readonly BufferedMessageStream _stream;

    public PeerWireReader(BufferedMessageStream stream)
    {
        _stream = stream;
    }

    public async Task<MessageFrameReader> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        var message = await _stream.ReadMessageAsync(cancellationToken);
        await message.EnsureReadAtleastAsync(1, cancellationToken);
        var reader = new BigEndianBinaryReader(message);
        var type = (MessageType)reader.ReadByte();
        var frame = new MessageFrameReader(message, type);
        await message.EnsureReadAtleastAsync(int.Min(message.AvailableBuffer, message.Remaining), cancellationToken);
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
