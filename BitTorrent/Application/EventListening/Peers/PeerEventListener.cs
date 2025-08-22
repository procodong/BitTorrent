using BitTorrentClient.BitTorrent.Peers.Errors;
using BitTorrentClient.Models.Messages;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Protocol.Networking.PeerWire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.EventListening.Peers;
internal class PeerEventListener
{
    private readonly PeerWireReader _connection;
    private readonly IPeerEventHandler _handler;

    public PeerEventListener(PeerWireReader connection, IPeerEventHandler handler)
    {
        _connection = connection;
        _handler = handler;
    }

    public async Task ListenAsync(ChannelReader<int> haveMessageReader, ChannelReader<PeerRelation> relationReader, CancellationToken cancellationToken = default)
    {
        Task<MessageFrameReader> receiveTask = _connection.ReceiveAsync(cancellationToken);
        Task<PeerRelation> relationTask = relationReader.ReadAsync(cancellationToken).AsTask();
        Task<int> haveTask = haveMessageReader.ReadAsync(cancellationToken).AsTask();
        while (true)
        {
            var readyTask = await Task.WhenAny(receiveTask, relationTask, haveTask);
            if (readyTask == receiveTask)
            {
                var message = await receiveTask;
                await HandleMessage(message, cancellationToken);
                receiveTask = _connection.ReceiveAsync(cancellationToken);
            }
            else if (readyTask == relationTask)
            {
                PeerRelation relation = await relationTask;
                await _handler.OnClientRelationAsync(relation.Choked ? Relation.Choke : Relation.Unchoke, cancellationToken);
                await _handler.OnClientRelationAsync(relation.Interested ? Relation.Interested : Relation.NotInterested, cancellationToken);
                relationTask = relationReader.ReadAsync(cancellationToken).AsTask();
            }
            else if (readyTask == haveTask)
            {
                int have = await haveTask;
                await _handler.OnClientHaveAsync(have, cancellationToken);
                haveTask = haveMessageReader.ReadAsync(cancellationToken).AsTask();
            }
        }
    }

    private async Task HandleMessage(MessageFrameReader message, CancellationToken cancellationToken = default)
    {
        switch (message.Type)
        {
            case MessageType.Choke:
                await _handler.OnPeerRelationAsync(Relation.Choke, cancellationToken);
                break;
            case MessageType.Unchoke:
                await _handler.OnPeerRelationAsync(Relation.Unchoke, cancellationToken);
                break;
            case MessageType.Interested:
                await _handler.OnPeerRelationAsync(Relation.Interested, cancellationToken);
                break;
            case MessageType.NotInterested:
                await _handler.OnPeerRelationAsync(Relation.NotInterested, cancellationToken);
                break;
            case MessageType.Have:
                var have = await message.ReadHaveAsync(cancellationToken);
                await _handler.OnPeerHaveAsync(have, cancellationToken);
                break;
            case MessageType.Bitfield:
                await _handler.OnBitfieldAsync(message.ReadStream(), cancellationToken);
                break;
            case MessageType.Request:
                var request = await message.ReadRequestAsync(cancellationToken);
                await _handler.OnRequestAsync(request, cancellationToken);
                break;
            case MessageType.Piece:
                var piece = await message.ReadPieceHeaderAsync(cancellationToken);
                await _handler.OnPieceAsync(new(piece, message.ReadStream()), cancellationToken);
                break;
            case MessageType.Cancel:
                var cancel = await message.ReadPieceHeaderAsync(cancellationToken);
                await _handler.OnCancelAsync(cancel, cancellationToken);
                break;
            default:
                throw new BadPeerException(PeerErrorReason.InvalidProtocol);
        }
    }
}