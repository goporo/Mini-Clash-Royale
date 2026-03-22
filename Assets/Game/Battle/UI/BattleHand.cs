using UnityEngine;
using ClashShared;


public class BattleHand : MonoBehaviour
{
  public static BattleHand Instance { get; private set; }

  [Header("Card Library (all possible CardConfig assets)")]
  public CardConfig[] cardLibrary;

  [Header("Hand Slots (4)")]
  public CardSlotUI[] handSlots = new CardSlotUI[4];

  [Header("Next Card Preview (optional)")]
  public CardSlotUI nextCardSlot;

  private int _pendingSlotIndex = -1;
  private CardConfig _pendingSlotConfig;

  private void Awake()
  {
    Instance = this;
  }

  public void InitHand(CardId card0, CardId card1, CardId card2, CardId card3, CardId nextCardId)
  {
    handSlots[0].SetCard(GetConfig(card0));
    handSlots[1].SetCard(GetConfig(card1));
    handSlots[2].SetCard(GetConfig(card2));
    handSlots[3].SetCard(GetConfig(card3));
    if (nextCardSlot != null)
      nextCardSlot.SetCard(GetConfig(nextCardId));
  }

  public void OnLocalCardPlayed(int slotIndex)
  {
    _pendingSlotIndex = slotIndex;
    _pendingSlotConfig = handSlots[slotIndex].Config;
    handSlots[slotIndex].SetCard(null);
  }

  public void RestorePendingSlot()
  {
    if (_pendingSlotIndex >= 0)
      handSlots[_pendingSlotIndex].SetCard(_pendingSlotConfig);
    _pendingSlotIndex = -1;
    _pendingSlotConfig = null;
  }

  public void OnServerCardDrawn(CardId playedCardId, CardId newCardId, CardId nextCardId)
  {
    // If we optimistically cleared a slot, fill it with the confirmed drawn card.
    // Otherwise, find the slot that still shows the played card (e.g. after reconnect).
    int slot = _pendingSlotIndex >= 0
      ? _pendingSlotIndex
      : FindSlotWithCard(playedCardId);

    _pendingSlotIndex = -1;

    if (slot >= 0)
      handSlots[slot].SetCard(GetConfig(newCardId));
    if (nextCardSlot != null)
      nextCardSlot.SetCard(GetConfig(nextCardId));
  }

  private CardConfig GetConfig(CardId cardId)
  {
    if (cardLibrary == null) return null;
    foreach (var c in cardLibrary)
      if (c != null && c.CardId == cardId) return c;
    return null;
  }

  private int FindSlotWithCard(CardId cardId)
  {
    for (int i = 0; i < handSlots.Length; i++)
      if (handSlots[i].Config != null && handSlots[i].Config.CardId == cardId) return i;
    return -1;
  }
}