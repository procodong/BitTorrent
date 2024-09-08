namespace BitTorrent.Models;
public readonly record struct Message(MessageType Type, Memory<byte> Data);