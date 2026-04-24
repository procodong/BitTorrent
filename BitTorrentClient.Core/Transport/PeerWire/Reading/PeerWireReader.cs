using System.Buffers;
using System.IO.Pipelines;
using BitTorrentClient.Core.Presentation.PeerWire;
using BitTorrentClient.Core.Presentation.PeerWire.Models;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Helpers.Streams;

namespace BitTorrentClient.Core.Transport.PeerWire.Reading;
public sealed class PeerWireReader : IPeerWireReader
{
    private readonly PipeReader _reader;
    private readonly int _bitfieldSize;

    public PeerWireReader(PipeReader reader, int bitfieldSize)
    {
        _reader = reader;
        _bitfieldSize = bitfieldSize;
    }

    public async Task<(MessageType, MessageData)> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        var data = await _reader.ReadAtLeastAsync(MessageDecoder.HeaderLen, cancellationToken);
        var reader = new SequenceReader<byte>(data.Buffer);
        var header = MessageDecoder.DecodeHeader(ref reader);
        var expectedSize = MessageDecoder.GetExpectedMessageLength(header.Type, _bitfieldSize);
        if (header.Length != expectedSize)
        {
            throw new BadPacketException(header.Length, expectedSize, header.Type);
        }
        if (header.Type != MessageType.Block)
        {
            _reader.AdvanceTo(reader.Position);
            data = await _reader.ReadAtLeastAsync(expectedSize, cancellationToken);
            reader = new SequenceReader<byte>(data.Buffer);
        }
        reader = new SequenceReader<byte>(reader.Sequence.Slice(0, header.Length));
        MessageData message = ReadMessage(ref reader, header.Type);
        _reader.AdvanceTo(reader.Position);
        return (header.Type, message);
    }

    private MessageData ReadMessage(ref SequenceReader<byte> reader, MessageType type)
    {
        switch (type)
        {
            case MessageType.Have:
                reader.TryReadBigEndian(out int piece);
                return new() { PieceIndex = piece };
            case MessageType.Bitfield:
                return new() { Bitfield = new(reader.ReadBytes((int)reader.Remaining)) };
            case MessageType.Request:
            case MessageType.Cancel:
                return new() { Request = MessageDecoder.DecodeRequest(ref reader) };
            case MessageType.Block:
                var data = MessageDecoder.DecodePieceHeader(ref reader);
                _reader.AdvanceTo(reader.Position);
                var stream = new LimitedStream(_reader.AsStream(), (int)reader.Remaining);
                var block = new BlockData(new(data.Index, data.Begin, (int)reader.Remaining), stream);
                return new() { Block = block };
        }
        return default;
    }

    public void Dispose()
    {
        _reader.Complete();
    }

    public ValueTask DisposeAsync()
    {
        return _reader.CompleteAsync();
    }
}

public class BadPacketException(int found, int expected, MessageType type) : Exception($"Expected message {type} of length {expected} found length {found}");