using UnityEngine;
using ClashShared;

public static class ClientCardPlayService
{
  public static bool TryPlayCard(CardConfig card, Vector2 position, int slotIndex)
  {
    if (card == null)
      return false;

    if (!ClientNetworkBridge.TrySendPlayCard(card.CardId, Vector2DataUnityConversions.FromUnityVector2(position)))
    {
      Debug.LogError("Client transport is not connected.");
      return false;
    }

    BattleHand.Instance?.OnLocalCardPlayed(slotIndex);
    return true;
  }
}
