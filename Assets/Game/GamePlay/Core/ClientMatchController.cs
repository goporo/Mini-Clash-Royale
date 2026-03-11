using Mirror;
using UnityEngine;
using ClashShared;

namespace ClashServer
{
  /// <summary>
  /// Client-only match controller - handles client-side state synchronization
  /// Uses Mirror directly for networking (static methods only - no NetworkBehaviour needed)
  /// </summary>
  public class ClientMatchController : MonoBehaviour
  {
    public static ClientMatchController Instance;

    private uint currentTick;

    void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(gameObject);
        return;
      }
      Instance = this;
      Debug.Log("[Client] ClientMatchController initialized");
    }

    // Called by MyNetworkManager when full snapshot is received
    public void OnFullSnapshotReceived(FullSnapshot snapshot)
    {
      Debug.Log($"[Client] Received full snapshot: Tick {snapshot.Tick}, Entities: {snapshot.Entities.Count}");
      currentTick = snapshot.Tick;

      ClientBoardState.RebuildFromSnapshot(snapshot);

      if (EntityViewManager.Instance != null)
      {
        EntityViewManager.Instance.ApplyFullSnapshot(snapshot);
      }
    }

    // Called by MyNetworkManager when delta snapshot is received
    public void OnDeltaSnapshotReceived(DeltaSnapshot delta)
    {
      // Debug.Log($"[Client] Received delta snapshot: +{delta.SpawnedEntities.Count} -{delta.DestroyedEntityIds.Count} ~{delta.UpdatedEntities.Count}");
      currentTick = delta.Tick;

      foreach (var e in delta.SpawnedEntities)
        ClientBoardState.PlaceBuilding(e);
      foreach (var id in delta.DestroyedEntityIds)
        ClientBoardState.RemoveBuildingById(id);

      if (EntityViewManager.Instance != null)
      {
        EntityViewManager.Instance.ApplyDeltaSnapshot(delta);
      }
    }

    // Called by MyNetworkManager when play card fails
    public void OnPlayCardFailed(string reason)
    {
      Debug.Log($"[Client] Play card failed: {reason}");
      BattleHand.Instance?.RestorePendingSlot();
    }

    public void OnMatchEnded(EntityTeam winner)
    {
      Debug.Log($"[Client] Match ended, winner: {winner}");
    }

    public void OnElixirUpdated(int milliElixir)
    {
      if (BattleUI.Instance != null)
        BattleUI.Instance.UpdateElixir(milliElixir);
    }

    public void OnHandStateReceived(HandStateMessage msg)
    {
      if (BattleHand.Instance != null)
        BattleHand.Instance.InitHand(msg.Card0, msg.Card1, msg.Card2, msg.Card3, msg.NextCardId);
    }

    public void OnCardDrawn(CardDrawnMessage msg)
    {
      if (BattleHand.Instance != null)
        BattleHand.Instance.OnServerCardDrawn(msg.PlayedCardId, msg.NewCardId, msg.NextCardId);
    }

    public void PlayCard(CardId cardId, Vector2 position)
    {
      if (!NetworkClient.isConnected)
      {
        Debug.LogWarning("[Client] Cannot play card - not connected");
        return;
      }

      NetworkClient.Send(new PlayCardMessage
      {
        CardId = cardId,
        Position = Vector2Data.FromUnityVector2(position)
      });

      Debug.Log($"[Client] Sent play card request: {cardId} at {position}");
    }

    void OnDestroy()
    {
      if (Instance == this)
      {
        Instance = null;
      }
    }
  }
}
