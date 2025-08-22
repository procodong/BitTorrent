namespace BitTorrentClient.Models.Messages;
public readonly record struct MessageHeader(int Length, MessageType Type);