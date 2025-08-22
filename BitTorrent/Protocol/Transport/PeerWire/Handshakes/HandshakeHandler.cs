using System.IO.Pipelines;
using BitTorrentClient.Application.Infrastructure.Peers.Exceptions;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Helpers.Streams;
using BitTorrentClient.Models.Messages;
using BitTorrentClient.Protocol.Presentation.PeerWire;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes.Exceptions;
using BitTorrentClient.Protocol.Transport.PeerWire.Reading;
using BitTorrentClient.Protocol.Transport.PeerWire.Sending;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

public class HandshakeHandler
{
    private const string Protocol = "BitTorrent protocol";

    private readonly PipeWriter _writer;
    private readonly BufferCursor _cursor;
    private readonly Stream _stream;
    private HandshakeData? _myHandshake;
    private HandshakeData? _otherHandshake;
    private LazyBitArray? _bitfield;

    public HandshakeData? ReceivedHandShake => _otherHandshake;
    public HandshakeData? SentHandShake => _myHandshake;

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
        if (_otherHandshake is not null) throw new AlreadyUsedException();
        await _stream.ReadAtLeastAsync(_cursor, MessageDecoder.HandshakeLen, cancellationToken: cancellationToken);
        HandShake receivedHandshake = MessageDecoder.DecodeHandShake(Reader);
        _otherHandshake = new HandshakeData(receivedHandshake.Extensions, receivedHandshake.InfoHash, receivedHandshake.PeerId);
        if (receivedHandshake.Protocol != Protocol)
        {
            throw new BadPeerException(PeerErrorReason.InvalidProtocol);
        }
    }

    public PeerWireStream Finish()
    {
        var stream = new BufferedMessageStream(_stream, _cursor);
        var reader = new PeerWireReader(stream);
        var sender = new PipedMessageSender(_writer);
        return new(ReceivedHandShake!.Value, reader, sender);
    }
}