using BitTorrentClient.Models.Messages;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Protocol.Networking.PeerWire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using BitTorrentClient.Application.Infrastructure.Peers.Exceptions;
using BitTorrentClient.Application.Events.EventListening;

namespace BitTorrentClient.Application.Events.EventListening.Peers;
internal class PeerEventListener : IEventListener
{
    private readonly IPeerWireReader _connection;
    private readonly IPeerEventHandler _handler;
    private readonly ChannelReader<int> _haveMessageReader;
    private readonly ChannelReader<PeerRelation> _relationReader;

    public PeerEventListener(IPeerWireReader connection, IPeerEventHandler handler, ChannelReader<int> haveMessageReader, ChannelReader<PeerRelation> relationReader)
    {
        _connection = connection;
        _handler = handler;
        _haveMessageReader = haveMessageReader;
        _relationReader = relationReader;
    }

    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        Task<IMessageFrameReader> receiveTask = _connection.ReceiveAsync(cancellationToken);
        Task<PeerRelation> relationTask = _relationReader.ReadAsync(cancellationToken).AsTask();
        Task<int> haveTask = _haveMessageReader.ReadAsync(cancellationToken).AsTask();
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
                await _handler.OnClientRelationAsync(relation, cancellationToken);
                relationTask = _relationReader.ReadAsync(cancellationToken).AsTask();
            }
            else if (readyTask == haveTask)
            {
                int have = await haveTask;
                await _handler.OnClientHaveAsync(have, cancellationToken);
                haveTask = _haveMessageReader.ReadAsync(cancellationToken).AsTask();
            }
        }
    }

    private async Task HandleMessage(IMessageFrameReader message, CancellationToken cancellationToken = default)
    {
        switch (message.Type)
        {
            case MessageType.Choke:
                await _handler.OnPeerRelationAsync(RelationUpdate.Choke, cancellationToken);
                break;
            case MessageType.UnChoke:
                await _handler.OnPeerRelationAsync(RelationUpdate.Unchoke, cancellationToken);
                break;
            case MessageType.Interested:
                await _handler.OnPeerRelationAsync(RelationUpdate.Interested, cancellationToken);
                break;
            case MessageType.NotInterested:
                await _handler.OnPeerRelationAsync(RelationUpdate.NotInterested, cancellationToken);
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
                var piece = await message.ReadPieceAsync(cancellationToken);
                await _handler.OnPieceAsync(piece, cancellationToken);
                break;
            case MessageType.Cancel:
                var cancel = await message.ReadRequestAsync(cancellationToken);
                await _handler.OnCancelAsync(cancel, cancellationToken);
                break;
            default:
                throw new BadPeerException(PeerErrorReason.InvalidProtocol);
        }
    }
}