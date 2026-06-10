using Mirror;
using ClashShared;

namespace ClashServer
{
  public class PlayerState
  {
    public NetworkConnectionToClient Connection { get; }
    public EntityTeam Team { get; }
    public ElixirState ElixirState { get; }
    public BattleDeck Deck { get; }
    public uint LastSeenPlayRequestId { get; private set; }

    public PlayerState(NetworkConnectionToClient conn, EntityTeam team)
    {
      Connection = conn;
      Team = team;
      ElixirState = new ElixirState();
      Deck = new BattleDeck(new[]
      {
        CardId.SkeletonBarrel, CardId.WallBreakers, CardId.Skeletons, CardId.SpearGoblins,
        CardId.Minions, CardId.GoblinGang, CardId.Giant, CardId.Bomber
      });
      LastSeenPlayRequestId = 0;
    }

    public bool CanAffordCard(CardId cardId)
    {
      return ElixirState.CanSpend(CardCostTable.GetMilliElixirCost(cardId));
    }

    public bool TrySpendElixir(CardId cardId)
    {
      return ElixirState.TrySpend(CardCostTable.GetMilliElixirCost(cardId));
    }

    public bool TryPlayCardFromHand(CardId cardId, out CardId drawnCardId)
    {
      return Deck.TryPlay(cardId, out drawnCardId);
    }

    public bool TryRegisterPlayRequest(uint requestId, out string failureReason)
    {
      if (requestId == 0)
      {
        failureReason = "Invalid play request";
        return false;
      }

      if (requestId <= LastSeenPlayRequestId)
      {
        failureReason = "Duplicate or stale play request";
        return false;
      }

      LastSeenPlayRequestId = requestId;
      failureReason = null;
      return true;
    }
  }
}
