using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Helpers.Streams;
using BitTorrentClient.Protocol.Presentation.PeerWire;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Reading;
public sealed class MessageFrameReader : IMessageFrameReader
{
    private readonly FrameReader _reader;
    private readonly MessageType _type;

    public MessageFrameReader(FrameReader reader, MessageType type)
    {
        _reader = reader;
        _type = type;
    }

    private BigEndianBinaryReader Reader => new(_reader);
    public MessageType Type => _type;

    public async Task<int> ReadHaveAsync(CancellationToken cancellationToken = default)
    {
        await _reader.EnsureReadAtLeastAsync(4, cancellationToken);
        var index = Reader.ReadInt32();
        return index;
    }

    public async Task<BlockRequest> ReadRequestAsync(CancellationToken cancellationToken = default)
    {
        await _reader.EnsureReadAtLeastAsync(12, cancellationToken);
        return MessageDecoder.DecodeRequest(Reader);
    }

    public async Task<BlockData> ReadPieceAsync(CancellationToken cancellationToken = default)
    {
        await _reader.EnsureReadAtLeastAsync(8, cancellationToken);
        var piece = MessageDecoder.DecodePieceHeader(Reader);
        var pieceRequest = new BlockRequest(piece.Index, piece.Begin, _reader.Remaining);
        return new(pieceRequest, _reader.GetStream());
    }

    public Stream ReadStream()
    {
        return _reader.GetStream();
    }
}
