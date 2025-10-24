using BitTorrentClient.Core.Presentation.PeerWire;
using BitTorrentClient.Core.Presentation.PeerWire.Models;
using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Helpers.Streams;

namespace BitTorrentClient.Core.Transport.PeerWire.Reading;
public sealed class MessageFrameReader : IMessageFrameReader
{
    private readonly FrameReader _reader;
    private readonly MessageType _type;
    private readonly SemaphoreSlim _readLock;

    public MessageFrameReader(FrameReader reader, MessageType type, SemaphoreSlim readLock)
    {
        _reader = reader;
        _type = type;
        _readLock = readLock;
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

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_reader.Remaining != 0)
            {
                await _reader.ReadToEndAsync();
            }
        }
        finally
        {
            _readLock.Release();
        }
    }

}
