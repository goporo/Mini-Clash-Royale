using Mirror;
using UnityEngine;
using ClashServer;

public class PlayerNetwork : NetworkBehaviour
{
  // Pure client class representing a player

  [Header("Client State")]
  [SyncVar] public float currentElixir;
  [SyncVar] public float maxElixir = 10f;

  public static PlayerNetwork LocalPlayer;

  public override void OnStartLocalPlayer()
  {
    LocalPlayer = this;
    Debug.Log("[Client] Local player started");
  }

  // Called from client UI when player wants to play a card
  [Command]
  public void CmdPlayCard(int cardId, string type, Vector2 position)
  {
    if (MatchController.Instance == null)
    {
      Debug.LogError("[Server] MatchController not found");
      return;
    }

    MatchController.Instance.Server_PlayCard(
        connectionToClient, cardId, type, position);
  }

  // Optional: Request current elixir update from server
  [Command]
  public void CmdRequestElixirUpdate()
  {
    // Server can send back current elixir
    // This is handled automatically by SyncVar, but you can force refresh
  }

  // Client can call this when they want to see their stats
  public void LogPlayerState()
  {
    Debug.Log($"[Client] Elixir: {currentElixir}/{maxElixir}");
  }
}

