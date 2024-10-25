using BitTorrent.Files.Streams;

namespace BitTorrent.Models.Messages;
public readonly record struct Message(MessageType Type, LimitedStream Stream);