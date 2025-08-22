namespace BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

public interface IInitializationPhase;
public abstract class InitialSendDataPhase : IInitializationPhase;
public abstract class InitialReadDataPhase : IInitializationPhase;
public abstract class SendDataPhase : IInitializationPhase;
public abstract class ReadDataPhase : IInitializationPhase;

