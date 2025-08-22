using BitTorrentClient.BitTorrent.Peers.Errors;
using BitTorrentClient.BitTorrent.Peers.Parsing;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Helpers.Streams;
using BitTorrentClient.Models.Messages;

namespace BitTorrentClient.BitTorrent.Peers.Connections;
public class PeerWireReader : IDisposable, IAsyncDisposable
{
    private readonly BufferedMessageStream _stream;
    
    public PeerWireReader(BufferedMessageStream stream)
    {
        _stream = stream;
    }

    public async Task ReceiveAsync(IPeerEventHandler eventHandler, CancellationToken cancellationToken = default)
    {
        var message = await _stream.ReadMessageAsync(cancellationToken);
        await message.EnsureReadAtleastAsync(1, cancellationToken);
        var reader = new BigEndianBinaryReader(message);
        var type = (MessageType)reader.ReadByte();
        switch (type)
        {
            case MessageType.Choke:
                await eventHandler.OnChokeAsync(cancellationToken);
                break;
            case MessageType.Unchoke:
                await eventHandler.OnUnChokedAsync(cancellationToken);
                break;
            case MessageType.Interested:
                await eventHandler.OnInterestedAsync(cancellationToken);
                break;
            case MessageType.NotInterested:
                await eventHandler.OnNotInterestedAsync(cancellationToken);
                break;
            case MessageType.Have:
                await message.EnsureReadAtleastAsync(4, cancellationToken);
                var index = reader.ReadInt32();
                await eventHandler.OnHaveAsync(index, cancellationToken);
                break;
            case MessageType.Bitfield:
                var buffer = await message.ReadToEndAsync(cancellationToken);
                var bitfield = new ZeroCopyBitArray(buffer);
                await eventHandler.OnBitfieldAsync(bitfield, cancellationToken);
                break;
            case MessageType.Request:
                await message.EnsureReadAtleastAsync(12, cancellationToken);
                var request = MessageDecoder.DecodeRequest(reader);
                await eventHandler.OnRequestAsync(request, cancellationToken);
                break;
            case MessageType.Piece:
                var piece = MessageDecoder.DecodePieceHeader(reader);
                var pieceRequest = new PieceRequest(piece.Index, piece.Begin, message.Remaining);
                await message.EnsureReadAtleastAsync(message.Remaining - 1024, cancellationToken);
                var stream = message.GetStream();
                await eventHandler.OnPieceAsync(new(pieceRequest, stream), cancellationToken);
                break;
            case MessageType.Cancel:
                await message.EnsureReadAtleastAsync(12, cancellationToken);
                PieceRequest cancel = MessageDecoder.DecodeRequest(reader);
                await eventHandler.OnCancelAsync(cancel, cancellationToken);
                break;
            default:
                throw new BadPeerException(PeerErrorReason.InvalidRequest);
        }
    }

    public void Dispose()
    {
        _stream.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
    }
}
