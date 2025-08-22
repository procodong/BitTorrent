namespace BitTorrentClient.Models.Messages;
public readonly record struct Message(int Length, MessageType Type);