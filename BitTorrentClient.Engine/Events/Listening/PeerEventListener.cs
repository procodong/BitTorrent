using System.Threading.Channels;
using BitTorrentClient.Engine.Events.Handling.Interface;
using BitTorrentClient.Engine.Events.Listening.Interface;
using BitTorrentClient.Engine.Infrastructure.Peers.Exceptions;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;
using BitTorrentClient.Protocol.Transport.PeerWire.Reading;

namespace BitTorrentClient.Engine.Events.Listening;
public sealed class PeerEventListener : IEventListener
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
        var taskListener = new TaskListener<EventType>(cancellationToken);
        taskListener.AddTask(EventType.Receive, _connection.ReceiveAsync(cancellationToken));
        taskListener.AddTask(EventType.TransferLimit, () => _transferLimitReader.ReadAsync(cancellationToken).AsTask());
        taskListener.AddTask(EventType.Have, () => _haveMessageReader.ReadAsync(cancellationToken).AsTask());
        while (true)
        {
            var (eventType, readyTask) = await taskListener.WaitAsync();
            switch (eventType)
            {
                case EventType.Receive:
                    var message = await (Task<IMessageFrameReader>)readyTask;
                    await HandleMessage(message, cancellationToken);
                    taskListener.AddTask(EventType.Receive, _connection.ReceiveAsync(cancellationToken));
                    break;
                case EventType.TransferLimit:
                    var transferLimit = await (Task<DataTransferVector>)readyTask;
                    await _handler.OnClientRelationAsync(transferLimit, cancellationToken);
                    break;
                case EventType.Have:
                    var have = await (Task<int>)readyTask;
                    await _handler.OnClientHaveAsync(have, cancellationToken);
                    break;

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

    enum EventType
    {
        Receive,
        TransferLimit,
        Have
    }
}