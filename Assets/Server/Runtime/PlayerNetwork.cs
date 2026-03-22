using Mirror;
using UnityEngine;
using ClashShared;

public class PlayerNetwork : NetworkBehaviour
{

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
      Position = Vector2DataUnityConversions.FromUnityVector2(position)
    });
  }
}
