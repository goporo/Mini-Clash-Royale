namespace ClashMeta
{
  // Holds cross-panel state for the "pick a collection card → replace a deck slot" flow.
  public static class DeckEditSession
  {
    public static bool IsActive { get; private set; }
    public static int PendingCardId { get; private set; }

    // Set by ConfirmSlot, consumed by DeckGridView on next OnEnable
    public static bool HasPendingSwap { get; private set; }
    public static int PendingSlotIndex { get; private set; }

    public static void BeginPickSlot(int cardId)
    {
      IsActive = true;
      PendingCardId = cardId;
      HasPendingSwap = false;
    }

    // Called by DeckCardView when user taps a slot
    public static void ConfirmSlot(int slotIndex)
    {
      if (!IsActive) return;
      IsActive = false;
      HasPendingSwap = true;
      PendingSlotIndex = slotIndex;
    }

    // Called by DeckGridView after it has consumed the pending swap
    public static void ConsumeSwap()
    {
      HasPendingSwap = false;
    }

    public static void Cancel()
    {
      IsActive = false;
      HasPendingSwap = false;
    }
  }
}
