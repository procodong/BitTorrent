using System.Buffers;
using System.IO.Pipelines;
using BitTorrentClient.Core.Presentation.PeerWire;
using BitTorrentClient.Core.Presentation.PeerWire.Models;
using BitTorrentClient.Core.Transport.PeerWire.Handshakes.Exceptions;
using BitTorrentClient.Core.Transport.PeerWire.Handshakes.Interface;
using BitTorrentClient.Core.Transport.PeerWire.Reading;
using BitTorrentClient.Core.Transport.PeerWire.Sending;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Parsing;

namespace BitTorrentClient.Core.Transport.PeerWire.Handshakes;

public sealed class HandshakeHandler : IHandshakeHandler
{
    private const string Protocol = "BitTorrent protocol";

    private readonly PipeWriter _writer;
    private readonly PipeReader _reader;
    private HandshakeData? _myHandshake;
    private LazyBitArray? _bitfield;
    private bool _finished;

    public HandshakeData? ReceivedHandshake { get; private set; }

    public HandshakeHandler(PipeReader reader, PipeWriter writer)
    {
        _writer = writer;
        _reader = reader;
    }

    private BigEndianBinaryWriter Writer => new(_writer);

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
        var result = await _reader.ReadAtLeastAsync(MessageDecoder.HandshakeLen, cancellationToken: cancellationToken);
        var reader = new SequenceReader<byte>(result.Buffer);
        var receivedHandshake = MessageDecoder.DecodeHandShake(ref reader);
        ReceivedHandshake = new HandshakeData(receivedHandshake.Extensions, receivedHandshake.InfoHash, receivedHandshake.PeerId);
        if (receivedHandshake.Protocol != Protocol)
        {
            throw new InvalidConnectionException();
        }
    }

    public PeerWireStream Finish()
    {
        var reader = new PeerWireReader(_reader, _bitfield!.Buffer.Length);
        var sender = new PipedPeerWireWriter(_writer);
        _finished = true;
        return new(ReceivedHandshake!.Value, reader, sender);
    }

    public void Dispose()
    {
        if (!_finished)
        {
            _reader.Complete();
            _writer.Complete();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_finished)
        {
            await _reader.CompleteAsync();
            await _writer.CompleteAsync();
        }
    }
}