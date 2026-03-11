using Mirror;
using UnityEngine;
using ClashServer;
using ClashShared;

public class PlayerNetwork : NetworkBehaviour
{
  // Pure client class representing a player

  public static PlayerNetwork LocalPlayer;

  public override void OnStartLocalPlayer()
  {
    LocalPlayer = this;
    Debug.Log("[Client] Local player started");
  }

  public void PlayCard(CardId cardId, Vector2 position)
  {
    if (!isLocalPlayer) return;

    NetworkClient.Send(new PlayCardMessage
    {
      CardId = cardId,
      Position = Vector2Data.FromUnityVector2(position)
    });
  }
}

