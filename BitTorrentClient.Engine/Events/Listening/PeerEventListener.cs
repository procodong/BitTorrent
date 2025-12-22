using System.Threading.Channels;
using BitTorrentClient.Engine.Events.Handling.Interface;
using BitTorrentClient.Engine.Events.Listening.Interface;
using BitTorrentClient.Engine.Infrastructure.Peers.Exceptions;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Core.Presentation.PeerWire.Models;
using BitTorrentClient.Core.Transport.PeerWire.Reading;

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
        taskListener.AddTask(EventType.Receive, () => _connection.ReceiveAsync(cancellationToken));
        taskListener.AddTask(EventType.TransferLimit, () => _transferLimitReader.ReadAsync(cancellationToken).AsTask());
        taskListener.AddTask(EventType.Have, () => _haveMessageReader.ReadAsync(cancellationToken).AsTask());
        while (!cancellationToken.IsCancellationRequested)
        {
            var ready = await taskListener.WaitAsync();
            switch (ready.EventType)
            {
                case EventType.Receive:
                    {
                        var (type, message) = ready.GetValue<(MessageType, MessageData)>();
                        await HandleMessage(type, message, cancellationToken);
                    }
                    break;
                case EventType.TransferLimit:
                    {
                        var transferLimit = ready.GetValue<DataTransferVector>();
                        await _handler.OnClientRelationAsync(transferLimit, cancellationToken);
                    }
                    break;
                case EventType.Have:
                    {
                        var have = ready.GetValue<int>();
                        await _handler.OnClientHaveAsync(have, cancellationToken);
                    }
                    break;
            }
        }
    }

    private async Task HandleMessage(MessageType type, MessageData message, CancellationToken cancellationToken = default)
    {
        switch (type)
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
                await _handler.OnPeerHaveAsync(message.PieceIndex, cancellationToken);
                break;
            case MessageType.Bitfield:
                await _handler.OnBitfieldAsync(message.Bitfield, cancellationToken);
                break;
            case MessageType.Request:
                await _handler.OnRequestAsync(message.Request, cancellationToken);
                break;
            case MessageType.Block:
                await _handler.OnPieceAsync(message.Block, cancellationToken);
                break;
            case MessageType.Cancel:
                await _handler.OnCancelAsync(message.Request, cancellationToken);
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