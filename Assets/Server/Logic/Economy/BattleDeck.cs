using System.Collections.Generic;
using ClashShared;

namespace ClashServer
{
  public class BattleDeck
  {
    private readonly Queue<CardId> drawQueue = new();
    private readonly List<CardId> hand = new(4);

    public IReadOnlyList<CardId> Hand => hand;
    public CardId NextCardId => drawQueue.Peek();

    public BattleDeck(IEnumerable<CardId> deckCardIds)
    {
      foreach (var id in deckCardIds)
        drawQueue.Enqueue(id);

      for (int i = 0; i < 4; i++)
        hand.Add(drawQueue.Dequeue());
    }

    public CardId CardIdAt(int slotIndex) => hand[slotIndex];

    public bool IsInHand(CardId cardId) => hand.Contains(cardId);

    public bool TryPlay(CardId cardId, out CardId drawnCardId)
    {
      int idx = hand.IndexOf(cardId);
      if (idx < 0)
      {
        drawnCardId = CardId.None;
        return false;
      }

      drawQueue.Enqueue(cardId);
      drawnCardId = drawQueue.Dequeue();
      hand[idx] = drawnCardId;
      return true;
    }
  }
}