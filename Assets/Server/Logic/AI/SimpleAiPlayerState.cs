using ClashShared;

namespace ClashServer
{
  internal sealed class SimpleAiPlayerState
  {
    public EntityTeam Team => EntityTeam.Team2;
    public int PlayerId => 1;
    public ElixirState ElixirState { get; }
    public BattleDeck Deck { get; }

    public SimpleAiPlayerState()
    {
      ElixirState = new ElixirState();
      Deck = new BattleDeck(new[]
      {
        CardId.Giant,
        CardId.Musketeer,
        CardId.Valkyrie,
        CardId.Archer,
        CardId.Goblin,
        CardId.Cannon,
        CardId.Fireball,
        CardId.MiniPekka
      });
    }

    public bool CanAfford(CardId cardId)
    {
      return ElixirState.CanSpend(CardCostTable.GetMilliElixirCost(cardId));
    }

    public bool CommitPlay(CardId cardId)
    {
      if (!CanAfford(cardId))
        return false;

      if (!Deck.IsInHand(cardId))
        return false;

      if (!ElixirState.TrySpend(CardCostTable.GetMilliElixirCost(cardId)))
        return false;

      return Deck.TryPlay(cardId, out _);
    }
  }
}
