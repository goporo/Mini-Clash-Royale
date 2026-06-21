using Mirror;

namespace ClashShared
{
  public enum MatchMode
  {
    PvE,
    PvP
  }


  public struct ClientReadyMessage : NetworkMessage
  {
    public string MatchId;
    public string PlayerToken;
    public ushort[] Deck; // PvE only — 8 card ids; null/empty means server uses random
  }

  public struct MatchReadyMessage : NetworkMessage { }

  public struct FullSnapshotMessage : NetworkMessage
  {
    public FullSnapshot Snapshot;
  }

  public struct DeltaSnapshotMessage : NetworkMessage
  {
    public DeltaSnapshot Delta;
  }

  public struct PlayCardMessage : NetworkMessage
  {
    public uint RequestId;
    public CardId CardId;
    public Vector2Data Position;
  }

  public struct PlayCardFailedMessage : NetworkMessage
  {
    public string Reason;
  }

  public struct MatchEndedMessage : NetworkMessage
  {
    public EntityTeam Winner;
  }

  public struct ElixirUpdateMessage : NetworkMessage
  {
    public int MilliElixir;
  }

  public struct HandStateMessage : NetworkMessage
  {
    public CardId Card0;
    public CardId Card1;
    public CardId Card2;
    public CardId Card3;
    public CardId NextCardId;
    public EntityTeam LocalTeam;
  }

  public struct CardDrawnMessage : NetworkMessage
  {
    public CardId PlayedCardId;
    public CardId NewCardId;
    public CardId NextCardId;
  }

  public struct SpellCastMessage : NetworkMessage
  {
    public CardId CardId;
    public Vector2Data Position;
    public EntityTeam Team;
  }
}
