using Mirror;
using ClashShared;

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
    public const short ElixirUpdate = 1006;
    public const short HandState = 1007;
    public const short CardDrawn = 1008;
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
    public CardId CardId;
    public Vector2Data Position;
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

  // Server → Specific Client: Elixir value update
  public struct ElixirUpdateMessage : NetworkMessage
  {
    public int MilliElixir;
  }

  // Server → Specific Client: Initial hand state (sent on client ready)
  public struct HandStateMessage : NetworkMessage
  {
    public CardId Card0;
    public CardId Card1;
    public CardId Card2;
    public CardId Card3;
    public CardId NextCardId;
  }

  // Server → Specific Client: Card drawn after a successful play
  public struct CardDrawnMessage : NetworkMessage
  {
    public CardId PlayedCardId;
    public CardId NewCardId;
    public CardId NextCardId;
  }
}
