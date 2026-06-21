namespace ClashMeta
{
  public struct CardSelectedFromCollectionEvent : IGameEvent
  {
    public int CardId;
  }

  public struct DeckSlotPickedEvent : IGameEvent
  {
    public int SlotIndex;
  }

  public struct DeckPickCancelledEvent : IGameEvent { }

  public struct RemoveFromDeckEvent : IGameEvent
  {
    public int SlotIndex;
  }

  public struct DeckUpdatedEvent : IGameEvent { }

  public struct DeckTabChangedEvent : IGameEvent { public int DeckIndex; }


  public static class DeckEditEvents
  {
    public static void SelectCardFromCollection(int cardId)
      => GameplayEvents.Publish(new CardSelectedFromCollectionEvent { CardId = cardId });

    public static void PickDeckSlot(int slotIndex)
      => GameplayEvents.Publish(new DeckSlotPickedEvent { SlotIndex = slotIndex });

    public static void CancelPick()
      => GameplayEvents.Publish(new DeckPickCancelledEvent());

    public static void RemoveFromDeck(int slotIndex)
      => GameplayEvents.Publish(new RemoveFromDeckEvent { SlotIndex = slotIndex });

    public static void NotifyDeckUpdated()
      => GameplayEvents.Publish(new DeckUpdatedEvent());

    public static void NotifyDeckTabChanged(int deckIndex)
      => GameplayEvents.Publish(new DeckTabChangedEvent { DeckIndex = deckIndex });
  }
}
