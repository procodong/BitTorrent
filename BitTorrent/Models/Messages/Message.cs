using BitTorrent.Files.Streams;

namespace BitTorrent.Models.Messages;
public readonly record struct Message(int Length, MessageType Type);