using System.IO.Pipelines;
using BitTorrentClient.BitTorrent.Peers.Errors;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Models.Messages;
using BitTorrentClient.Protocol.Presentation.PeerWire;

namespace BitTorrentClient.Protocol.Networking.PeerWire;

public class PeerHandshaker : IPeerHandshaker
{
    private const string Protocol = "BitTorrent protocol";

    private readonly PipeWriter _writer;
    private readonly BufferCursor _cursor;
    private readonly Stream _stream;
    private HandShakeData? _myHandshake;
    private HandShakeData? _otherHandshake;

    public HandShakeData? ReceivedHandShake => _otherHandshake;
    public HandShakeData? SentHandShake => _myHandshake;

    public PeerHandshaker(Stream stream, BufferCursor cursor)
    {
        _stream = stream;
        _cursor = cursor;
        _writer = PipeWriter.Create(stream);
    }

    private BigEndianBinaryWriter Writer => new(_writer);
    private BigEndianBinaryReader Reader => new(_cursor);

    public async Task SendHandShakeAsync(HandShakeData handShake, CancellationToken cancellationToken = default)
    {
        _myHandshake = handShake;
        MessageEncoder.EncodeHandShake(Writer, new(Protocol, handShake.Extensions, handShake.InfoHash, handShake.PeerId));
        await _writer.FlushAsync(cancellationToken);
    }

    public virtual async Task SendBitfieldAsync(LazyBitArray bitfield, CancellationToken cancellationToken = default)
    {
        MessageEncoder.EncodeHeader(Writer, new(bitfield.Buffer.Length + 1, MessageType.Bitfield));
        await _writer.WriteAsync(bitfield.Buffer, cancellationToken);
    }

    public async Task<RespondedPeerHandshaker> ReadHandShakeAsync(CancellationToken cancellationToken = default)
    {
        await _stream.ReadAtLeastAsync(_cursor, MessageDecoder.HandshakeLen, cancellationToken: cancellationToken);
        HandShake receivedHandshake = MessageDecoder.DecodeHandShake(Reader);
        _otherHandshake = new HandShakeData(receivedHandshake.Extensions, receivedHandshake.InfoHash, receivedHandshake.PeerId);
        if (receivedHandshake.Protocol != Protocol)
        {
            throw new BadPeerException(PeerErrorReason.InvalidProtocol);
        }
        return new(this);
    }

    public (BufferCursor, Stream, PipeWriter) Finish() => (_cursor, _stream, _writer);
}