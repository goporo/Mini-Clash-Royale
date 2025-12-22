using Mirror;
using UnityEngine;
using System.Collections.Generic;

namespace ClashServer
{
  /// <summary>
  /// Server-authoritative match controller using snapshot-based synchronization.
  /// 
  /// Architecture:
  /// - Server simulates at TICK_RATE (10 Hz)
  /// - Snapshots sent at SNAPSHOT_RATE (10 Hz - can be lower than tick rate)
  /// - Clients interpolate between snapshots for smooth visuals
  /// - Only authoritative, non-derivable state is snapshotted (position, HP, alive state)
  /// </summary>
  public class MatchController : NetworkBehaviour
  {
    public static MatchController Instance;

    private GameplayDirector gameplayDirector;
    private MatchManager matchManager;
    private Dictionary<NetworkConnectionToClient, PlayerState> players;

    const float TICK_RATE = 0.1f;
    const float SNAPSHOT_RATE = 0.1f;  // 10 Hz - send snapshots less frequently than simulation
    float tickTimer;
    float snapshotTimer;

    public override void OnStartServer()
    {
      Debug.Log("[Server] MatchController started");
      NetworkServer.OnConnectedEvent += OnPlayerConnected;
      Instance = this;

      gameplayDirector = new GameplayDirector();
      matchManager = new MatchManager();
      players = new Dictionary<NetworkConnectionToClient, PlayerState>();
      InitializeMatch();
    }

    public override void OnStartClient()
    {
      Debug.Log("[Client] MatchController started");
      base.OnStartClient();
    }

    [Server]
    void InitializeMatch()
    {
      // Spawn towers for both teams
      var tower1 = gameplayDirector.SpawnEntity("kingtower", new Vector2(0, -18), EntityTeam.Team1, true);
      var tower2 = gameplayDirector.SpawnEntity("tower", new Vector2(-5, -15), EntityTeam.Team1, true);
      var tower3 = gameplayDirector.SpawnEntity("tower", new Vector2(5, -15), EntityTeam.Team1, true);

      var tower4 = gameplayDirector.SpawnEntity("kingtower", new Vector2(0, 18), EntityTeam.Team2, true);
      var tower5 = gameplayDirector.SpawnEntity("tower", new Vector2(-5, 15), EntityTeam.Team2, true);
      var tower6 = gameplayDirector.SpawnEntity("tower", new Vector2(5, 15), EntityTeam.Team2, true);

      // Notify clients to spawn tower visuals
      RpcSpawnEntity(tower1.Id, "kingtower", tower1.Position, EntityTeam.Team1, true);
      RpcSpawnEntity(tower2.Id, "tower", tower2.Position, EntityTeam.Team1, true);
      RpcSpawnEntity(tower3.Id, "tower", tower3.Position, EntityTeam.Team1, true);
      RpcSpawnEntity(tower4.Id, "kingtower", tower4.Position, EntityTeam.Team2, true);
      RpcSpawnEntity(tower5.Id, "tower", tower5.Position, EntityTeam.Team2, true);
      RpcSpawnEntity(tower6.Id, "tower", tower6.Position, EntityTeam.Team2, true);

      Debug.Log("[Server] Match initialized with towers");
    }

    [ServerCallback]
    void Update()
    {
      tickTimer += Time.deltaTime;
      while (tickTimer >= TICK_RATE)
      {
        tickTimer -= TICK_RATE;
        ServerTick(TICK_RATE);
      }

      // Send snapshots at a different rate (can be slower than tick rate)
      snapshotTimer += Time.deltaTime;
      if (snapshotTimer >= SNAPSHOT_RATE)
      {
        snapshotTimer -= SNAPSHOT_RATE;
        SyncStateToClients();
      }
    }

    [Server]
    void ServerTick(float deltaTime)
    {
      // Update elixir for all players
      // foreach (var playerState in players.Values)
      // {
      //   playerState.UpdateElixir(deltaTime);
      // }

      // Update gameplay simulation
      gameplayDirector.Update(deltaTime);

      // Update AI (if enabled)
      matchManager.UpdateAI(deltaTime, gameplayDirector);

      // Check for match end
      matchManager.UpdateMatchState(gameplayDirector);

      if (matchManager.IsMatchOver && matchManager.Winner.HasValue)
      {
        RpcMatchEnded(matchManager.Winner.Value);
      }

      // Note: Snapshots sent separately in Update() at SNAPSHOT_RATE
    }

    [Server]
    public void Server_PlayCard(
        NetworkConnectionToClient sender,
        int cardId,
        string type,
        Vector2 position)
    {
      // Validate sender exists
      if (!players.ContainsKey(sender))
      {
        Debug.LogWarning($"[Server] Unknown player tried to play card");
        return;
      }

      PlayerState playerState = players[sender];

      // Validate elixir cost
      if (!playerState.CanAffordCard(cardId))
      {
        Debug.LogWarning($"[Server] Player cannot afford card {cardId}");
        RpcPlayCardFailed(sender, "Not enough elixir");
        return;
      }

      // Validate position (optional - check if within player's side)
      if (!IsValidSpawnPosition(position, playerState.Team))
      {
        Debug.LogWarning($"[Server] Invalid spawn position {position}");
        RpcPlayCardFailed(sender, "Invalid position");
        return;
      }

      // Spend elixir
      playerState.SpendElixir(cardId);

      // Spawn unit in server simulation
      ServerEntity entity = gameplayDirector.SpawnEntity(type, position, playerState.Team);

      // Notify all clients to spawn visual
      RpcSpawnEntity(entity.Id, type, position, playerState.Team, false);
      Debug.Log($"[Server] Spawned {type} (id={entity.Id}) for player");
    }

    [Server]
    bool IsValidSpawnPosition(Vector2 position, EntityTeam team)
    {
      return true;
    }



    [Server]
    void SyncStateToClients()
    {
      // Collect all entity snapshots
      var entities = gameplayDirector.GetEntities();
      EntitySnapshot[] snapshots = new EntitySnapshot[entities.Count];

      for (int i = 0; i < entities.Count; i++)
      {
        snapshots[i] = EntitySnapshot.FromServerEntity(entities[i]);
      }

      // Send single batched RPC with all snapshots
      RpcApplySnapshot(snapshots);
    }

    [ClientRpc]
    void RpcApplySnapshot(EntitySnapshot[] snapshots)
    {
      // Client applies all snapshots at once
      if (EntityViewManager.Instance != null)
      {
        EntityViewManager.Instance.ApplySnapshot(snapshots);
      }
    }

    [ClientRpc]
    void RpcSpawnEntity(int entityId, string type, Vector2 position, EntityTeam team, bool isBuilding)
    {
      // Client spawns visual representation
      if (EntityViewManager.Instance != null)
      {
        EntityViewManager.Instance.SpawnEntity(entityId, 0, position, team, type, isBuilding);
      }
    }

    [TargetRpc]
    void RpcPlayCardFailed(NetworkConnection target, string reason)
    {
      Debug.Log($"[Client] Card play failed: {reason}");
    }

    [ClientRpc]
    void RpcMatchEnded(EntityTeam winner)
    {
      Debug.Log($"[Client] Match ended! Winner: {winner}");
    }



    public override void OnStopServer()
    {
      NetworkServer.OnConnectedEvent -= OnPlayerConnected;
    }

    void OnPlayerConnected(NetworkConnectionToClient conn)
    {
      Debug.Log($"[Server] Player cdmmmmmm {conn.connectionId} connected");

      // Assign team (Team1 for first player, Team2 for second)
      EntityTeam team = players.Count == 0 ? EntityTeam.Team1 : EntityTeam.Team2;

      PlayerState playerState = new PlayerState(conn, team);
      players[conn] = playerState;

      Debug.Log($"[Server] Player assigned to {team}");
    }


  }
}


