using System.Threading.Channels;
using BitTorrentClient.Application.Events.Listening.Interface;
using BitTorrentClient.Application.Events.Listening.Peers;
using BitTorrentClient.Application.Infrastructure.Peers.Exceptions;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;
using BitTorrentClient.Protocol.Transport.PeerWire.Reading;

namespace BitTorrentClient.Application.Events.Listening;
internal class PeerEventListener : IEventListener
{
    private readonly IPeerWireReader _connection;
    private readonly IPeerEventHandler _handler;
    private readonly ChannelReader<int> _haveMessageReader;
    private readonly ChannelReader<DataTransferVector> _transferLimitReader;

    public PeerEventListener(IPeerEventHandler handler, IPeerWireReader connection, ChannelReader<int> haveMessageReader, ChannelReader<DataTransferVector> transferLimitReader)
    {
        _connection = connection;
        _handler = handler;
        _haveMessageReader = haveMessageReader;
        _transferLimitReader = transferLimitReader;
    }

    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        Task<IMessageFrameReader> receiveTask = _connection.ReceiveAsync(cancellationToken);
        Task<DataTransferVector> transferLimitTask = _transferLimitReader.ReadAsync(cancellationToken).AsTask();
        Task<int> haveTask = _haveMessageReader.ReadAsync(cancellationToken).AsTask();
        while (true)
        {
            var readyTask = await Task.WhenAny(receiveTask, transferLimitTask, haveTask);
            if (readyTask == receiveTask)
            {
                var message = await receiveTask;
                await HandleMessage(message, cancellationToken);
                receiveTask = _connection.ReceiveAsync(cancellationToken);
            }
            else if (readyTask == transferLimitTask)
            {
                DataTransferVector relation = await transferLimitTask;
                await _handler.OnClientRelationAsync(relation, cancellationToken);
                transferLimitTask = _transferLimitReader.ReadAsync(cancellationToken).AsTask();
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