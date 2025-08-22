namespace BitTorrentClient.Protocol.Presentation.PeerWire.Models;
public readonly record struct MessageHeader(int Length, MessageType Type);