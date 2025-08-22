using System.IO.Pipelines;
using BitTorrentClient.BitTorrent.Peers.Errors;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Models.Messages;
using BitTorrentClient.Protocol.Presentation.PeerWire;

namespace BitTorrentClient.BitTorrent.Peers.Connections;

public class PeerHandshaker
{
    private const string Protocol = "BitTorrent protocol";
    
    private readonly PipeWriter _writer;
    private readonly Helpers.Parsing.BufferCursor _cursor;
    private readonly ArrayBufferReader _reader;
    private readonly Stream _stream;
    private HandShakeData? _myHandshake;
    private HandShakeData? _otherHandshake;

    public HandShakeData? ReceivedHandShake => _otherHandshake;
    public HandShakeData? SentHandShake => _myHandshake;

    public PeerHandshaker(Stream stream, int bufferSize)
    {
        var buffer = new byte[bufferSize];
        var cursor = new Helpers.Parsing.Buffer(buffer);
        _stream = stream;
        _cursor = cursor;
        _writer = PipeWriter.Create(stream);
        _reader = new(cursor);
    }

    private BigEndianBinaryWriter Writer => new(_writer);
    private BigEndianBinaryReader Reader => new(_reader);
    
    public async Task SendHandShakeAsync(HandShakeData handShake)
    {
        _myHandshake = handShake;
        MessageEncoder.EncodeHandShake(Writer, new(Protocol, handShake.Extensions, handShake.InfoHash, handShake.PeerId));
        await _writer.FlushAsync();
    }

    public virtual async Task SendBitfieldAsync(LazyBitArray bitfield)
    {
        MessageEncoder.EncodeHeader(Writer, new(bitfield.Buffer.Length + 1, MessageType.Bitfield));
        await _writer.WriteAsync(bitfield.Buffer);
    }

    public async Task<HandShake> ReadHandShakeAsync(CancellationToken cancellationToken = default)
    {
        await _stream.ReadAtLeastAsync(_cursor, MessageDecoder.HandshakeLen, cancellationToken: cancellationToken);
        HandShake receivedHandshake = MessageDecoder.DecodeHandShake(Reader);
        _otherHandshake = new HandShakeData(receivedHandshake.Extensions, receivedHandshake.InfoHash, receivedHandshake.PeerId);
        if (receivedHandshake.Protocol != Protocol)
        {
            throw new BadPeerException(PeerErrorReason.InvalidProtocol);
        }
        return receivedHandshake;
    }

    public (Buffer, Stream, PipeWriter) Finish() => (_cursor, _stream, _writer);
}