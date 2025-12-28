using Mirror;

namespace ClashServer
{
  // Message IDs - used to identify message types
  public static class MessageId
  {
    public const short FullSnapshot = 1000;
    public const short DeltaSnapshot = 1001;
    public const short PlayCard = 1002;
    public const short PlayCardFailed = 1003;
    public const short MatchEnded = 1004;
    public const short ClientReady = 1005;
  }

  // Client → Server: Client is ready to receive snapshots
  public struct ClientReadyMessage : NetworkMessage
  {
  }

  // Server → Specific Client: Full game state
  public struct FullSnapshotMessage : NetworkMessage
  {
    public FullSnapshot Snapshot;
  }

  // Server → All Clients: Incremental changes
  public struct DeltaSnapshotMessage : NetworkMessage
  {
    public DeltaSnapshot Delta;
  }

  // Client → Server: Play a card
  public struct PlayCardMessage : NetworkMessage
  {
    public int CardId;
    public string Type;
    public Vector2Data Position; // Use Vector2Data for network serialization
  }

  // Server → Specific Client: Card play failed
  public struct PlayCardFailedMessage : NetworkMessage
  {
    public string Reason;
  }

  // Server → All Clients: Match ended
  public struct MatchEndedMessage : NetworkMessage
  {
    public EntityTeam Winner;
  }
}
