using System.IO.Pipelines;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Helpers.Streams;
using BitTorrentClient.Protocol.Presentation.PeerWire;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes.Exceptions;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes.Interface;
using BitTorrentClient.Protocol.Transport.PeerWire.Reading;
using BitTorrentClient.Protocol.Transport.PeerWire.Sending;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

public class HandshakeHandler : IHandshakeHandler
{
    private const string Protocol = "BitTorrent protocol";

    private readonly PipeWriter _writer;
    private readonly BufferCursor _cursor;
    private readonly Stream _stream;
    private HandshakeData? _myHandshake;
    private LazyBitArray? _bitfield;
    private bool _finished;

    public HandshakeData? ReceivedHandshake { get; private set; }

    public HandshakeHandler(Stream stream, BufferCursor cursor)
    {
        _stream = stream;
        _cursor = cursor;
        _writer = PipeWriter.Create(stream);
    }

    private BigEndianBinaryWriter Writer => new(_writer);
    private BigEndianBinaryReader Reader => new(_cursor);

    public async Task SendHandShakeAsync(HandshakeData handshake, CancellationToken cancellationToken = default)
    {
        if (_myHandshake is not null) throw new AlreadyUsedException();
        _myHandshake = handshake;
        MessageEncoder.EncodeHandShake(Writer, new(Protocol, handshake.Extensions, handshake.InfoHash, handshake.PeerId));
        await _writer.FlushAsync(cancellationToken);
    }

    public async Task SendBitfieldAsync(LazyBitArray bitfield, CancellationToken cancellationToken = default)
    {
        if (_bitfield is not null) throw new AlreadyUsedException();
        _bitfield = bitfield;
        if (bitfield.NoneSet) return;
        MessageEncoder.EncodeHeader(Writer, new(bitfield.Buffer.Length + 1, MessageType.Bitfield));
        await _writer.WriteAsync(bitfield.Buffer, cancellationToken);
    }

    public async Task ReadHandShakeAsync(CancellationToken cancellationToken = default)
    {
        if (ReceivedHandshake is not null) throw new AlreadyUsedException();
        await _stream.ReadAtLeastAsync(_cursor, MessageDecoder.HandshakeLen, cancellationToken: cancellationToken);
        var receivedHandshake = MessageDecoder.DecodeHandShake(Reader);
        ReceivedHandshake = new HandshakeData(receivedHandshake.Extensions, receivedHandshake.InfoHash, receivedHandshake.PeerId);
        if (receivedHandshake.Protocol != Protocol)
        {
            throw new InvalidConnectionException();
        }
    }

    public PeerWireStream Finish()
    {
        var stream = new BufferedMessageStream(_stream, _cursor);
        var reader = new PeerWireReader(stream);
        var sender = new PipedMessageSender(_writer);
        _finished = true;
        return new(ReceivedHandshake!.Value, reader, sender);
    }

    public void Dispose()
    {
        if (!_finished)
        {
            _stream.Dispose();
        }
    }

    public ValueTask DisposeAsync()
    {
        if (!_finished)
        {
            return _stream.DisposeAsync();
        }
        return ValueTask.CompletedTask;
    }
}