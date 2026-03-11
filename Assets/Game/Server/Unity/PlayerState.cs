using Mirror;
using ClashShared;

namespace ClashServer
{
  public class PlayerState
  {
    public NetworkConnectionToClient Connection { get; set; }
    public EntityTeam Team { get; set; }
    public ElixirState ElixirState { get; set; }
    public BattleDeck Deck { get; set; }

    public PlayerState(NetworkConnectionToClient conn, EntityTeam team)
    {
      Connection = conn;
      Team = team;
      ElixirState = new ElixirState();
      Deck = new BattleDeck(new[]
      {
        CardId.Knight, CardId.Archer, CardId.Giant, CardId.Knight,
        CardId.Knight, CardId.Archer, CardId.Giant, CardId.Knight
      });
    }

    public bool CanAffordCard(CardId cardId)
    {
      return ElixirState.CanSpend(GetCardCostMilliElixir(cardId));
    }

    public void SpendElixir(CardId cardId)
    {
      ElixirState.TrySpend(GetCardCostMilliElixir(cardId));
    }

    private int GetCardCostMilliElixir(CardId cardId)
    {
      switch (cardId)
      {
        case CardId.Knight: return 3000;
        case CardId.Archer: return 3000;
        case CardId.Giant: return 5000;
        case CardId.Cannon: return 3000;
        case CardId.Fireball: return 4000;
        default: return 3000;
      }
    }
  }
}
