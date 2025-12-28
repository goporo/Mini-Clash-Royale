using Mirror;
using UnityEngine;
using ClashServer;

public class PlayerNetwork : NetworkBehaviour
{
  // Pure client class representing a player

  public static PlayerNetwork LocalPlayer;

  public override void OnStartLocalPlayer()
  {
    LocalPlayer = this;
    Debug.Log("[Client] Local player started");
  }

  public void PlayCard(int cardId, string type, Vector2 position)
  {
    if (!isLocalPlayer) return;

    NetworkClient.Send(new PlayCardMessage
    {
      CardId = cardId,
      Type = type,
      Position = Vector2Data.FromUnityVector2(position)
    });
  }
}

