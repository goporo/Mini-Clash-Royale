using Mirror;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ClashServer
{
  /// <summary>
  /// Server-only match controller - handles game logic and state
  /// Uses Mirror directly for networking
  /// </summary>
  [RequireComponent(typeof(NetworkIdentity))]
  public class ServerMatchController : NetworkBehaviour
  {
    public static ServerMatchController Instance;

    private GameplayDirector gameplayDirector;
    private MatchManager matchManager;
    private Dictionary<NetworkConnectionToClient, PlayerState> players;

    const float TICK_RATE = 0.1f;
    const float SNAPSHOT_RATE = 0.1f;
    float tickTimer;
    float snapshotTimer;

    private HashSet<NetworkConnectionToClient> clientsNeedingFullSnapshot = new HashSet<NetworkConnectionToClient>();

    public override void OnStartServer()
    {
      Debug.Log("[Server] ServerMatchController started");
      Instance = this;

      NetworkServer.OnConnectedEvent += HandlePlayerConnected;
      NetworkServer.OnDisconnectedEvent += HandlePlayerDisconnected;

      var logger = new UnityLogger();
      gameplayDirector = new GameplayDirector(logger);
      matchManager = new MatchManager(logger);
      players = new Dictionary<NetworkConnectionToClient, PlayerState>();
      clientsNeedingFullSnapshot = new HashSet<NetworkConnectionToClient>();

      InitializeMatch();
    }

    public void HandleClientReady(NetworkConnectionToClient conn)
    {
      if (!clientsNeedingFullSnapshot.Contains(conn))
      {
        clientsNeedingFullSnapshot.Add(conn);
        Debug.Log($"[Server] Client {conn.connectionId} is ready, will receive full snapshot");
      }
    }

    private void InitializeMatch()
    {
      // Team 1 (bottom)
      gameplayDirector.SpawnEntity("kingtower", new System.Numerics.Vector2(0, -6), EntityTeam.Team1, true);
      gameplayDirector.SpawnEntity("tower", new System.Numerics.Vector2(-5, -3), EntityTeam.Team1, true);
      gameplayDirector.SpawnEntity("tower", new System.Numerics.Vector2(5, -3), EntityTeam.Team1, true);

      // Team 2 (top)
      gameplayDirector.SpawnEntity("kingtower", new System.Numerics.Vector2(0, 18), EntityTeam.Team2, true);
      gameplayDirector.SpawnEntity("tower", new System.Numerics.Vector2(-5, 15), EntityTeam.Team2, true);
      gameplayDirector.SpawnEntity("tower", new System.Numerics.Vector2(5, 15), EntityTeam.Team2, true);

      gameplayDirector.ResetSnapshotTracking();

      Debug.Log("[Server] Match initialized with king towers and arena towers");
    }

    [ServerCallback]
    private void Update()
    {
      tickTimer += Time.deltaTime;
      while (tickTimer >= TICK_RATE)
      {
        tickTimer -= TICK_RATE;
        ServerTick(TICK_RATE);
      }

      snapshotTimer += Time.deltaTime;
      if (snapshotTimer >= SNAPSHOT_RATE)
      {
        snapshotTimer -= SNAPSHOT_RATE;
        SyncStateToClients();
      }
    }

    private void ServerTick(float deltaTime)
    {
      gameplayDirector.Update(deltaTime);
      matchManager.UpdateAI(deltaTime, gameplayDirector);
      matchManager.UpdateMatchState(gameplayDirector);

      if (matchManager.IsMatchOver && matchManager.Winner.HasValue)
      {
        BroadcastMatchEnded(matchManager.Winner.Value);
      }
    }

    public void Server_PlayCard(
        NetworkConnectionToClient sender,
        int cardId,
        string type,
        System.Numerics.Vector2 position)
    {
      if (!players.ContainsKey(sender))
      {
        Debug.LogWarning($"[Server] Unknown player tried to play card");
        return;
      }

      PlayerState playerState = players[sender];

      if (!playerState.CanAffordCard(cardId))
      {
        Debug.LogWarning($"[Server] Player cannot afford card {cardId}");
        SendPlayCardFailed(sender, "Not enough elixir");
        return;
      }

      if (!IsValidSpawnPosition(position, playerState.Team))
      {
        Debug.LogWarning($"[Server] Invalid spawn position {position}");
        SendPlayCardFailed(sender, "Invalid position");
        return;
      }

      playerState.SpendElixir(cardId);
      ServerEntity entity = gameplayDirector.SpawnEntity(type, position, playerState.Team);
      Debug.Log($"[Server] Player played card {cardId}, spawned entity {entity.Id} ({type}) at {position}");
    }

    private bool IsValidSpawnPosition(System.Numerics.Vector2 position, EntityTeam team)
    {
      return true;
    }

    private void SyncStateToClients()
    {
      foreach (var conn in clientsNeedingFullSnapshot.ToList())
      {
        SendFullSnapshot(conn);
        clientsNeedingFullSnapshot.Remove(conn);
      }

      DeltaSnapshot delta = gameplayDirector.GenerateDeltaSnapshot();

      if (delta.SpawnedEntities.Count > 0 ||
          delta.DestroyedEntityIds.Count > 0 ||
          delta.UpdatedEntities.Count > 0)
      {
        BroadcastDeltaSnapshot(delta);
      }
    }

    private void SendFullSnapshot(NetworkConnectionToClient conn)
    {
      FullSnapshot fullSnapshot = gameplayDirector.GenerateFullSnapshot();
      conn.Send(new FullSnapshotMessage { Snapshot = fullSnapshot });
      Debug.Log($"[Server] Sent full snapshot to client {conn.connectionId} (Tick: {fullSnapshot.Tick}, Entities: {fullSnapshot.Entities.Count})");
    }

    private void BroadcastDeltaSnapshot(DeltaSnapshot delta)
    {
      NetworkServer.SendToAll(new DeltaSnapshotMessage { Delta = delta });
    }

    private void SendPlayCardFailed(NetworkConnectionToClient conn, string reason)
    {
      conn.Send(new PlayCardFailedMessage { Reason = reason });
    }

    private void BroadcastMatchEnded(EntityTeam winner)
    {
      NetworkServer.SendToAll(new MatchEndedMessage { Winner = winner });
    }

    public override void OnStopServer()
    {
      NetworkServer.OnConnectedEvent -= HandlePlayerConnected;
      NetworkServer.OnDisconnectedEvent -= HandlePlayerDisconnected;
      base.OnStopServer();
    }

    private void HandlePlayerConnected(NetworkConnectionToClient conn)
    {
      Debug.Log($"[Server] Player {conn.connectionId} connected");

      EntityTeam team = players.Count == 0 ? EntityTeam.Team1 : EntityTeam.Team2;
      PlayerState playerState = new PlayerState(conn, team);
      players[conn] = playerState;

      Debug.Log($"[Server] Player assigned to {team}, waiting for ready message");
    }

    private void HandlePlayerDisconnected(NetworkConnectionToClient conn)
    {
      Debug.Log($"[Server] Player {conn.connectionId} disconnected");
      if (players.ContainsKey(conn))
      {
        players.Remove(conn);
      }
      clientsNeedingFullSnapshot.Remove(conn);
    }
  }
}
