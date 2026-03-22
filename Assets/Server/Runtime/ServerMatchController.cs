using Mirror;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ClashShared;

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

    // Replay system
    private ReplayRecorder replayRecorder;
    private DriftDetector driftDetector;
    [SerializeField] private bool enableReplay = true;
    [SerializeField] private bool enableDriftDetection = true;
    [SerializeField] private string replayFolderPath = "Replays";

    private const float SNAPSHOT_RATE = ServerTickSettings.FixedDeltaTime;
    private float tickTimer;
    private float snapshotTimer;
    private int currentTick = 0;
    private float timeAccumulator = 0f;

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
      gameplayDirector.SetBoardManager(matchManager.BoardManager);
      players = new Dictionary<NetworkConnectionToClient, PlayerState>();
      clientsNeedingFullSnapshot = new HashSet<NetworkConnectionToClient>();

      if (enableReplay)
      {
        replayRecorder = new ReplayRecorder();

        if (!System.IO.Directory.Exists(replayFolderPath))
        {
          System.IO.Directory.CreateDirectory(replayFolderPath);
        }

        var metadata = new MatchMetadata("Player1", "AI");
        replayRecorder.StartRecording(metadata);
        Debug.Log("[Server] Replay recording started");
      }

      if (enableDriftDetection)
      {
        driftDetector = new DriftDetector(hashInterval: 10, logger: logger);
        Debug.Log("[Server] Drift detection enabled (every 10 ticks)");
      }

      InitializeMatch();
    }

    public void HandleClientReady(NetworkConnectionToClient conn)
    {
      if (!clientsNeedingFullSnapshot.Contains(conn))
      {
        clientsNeedingFullSnapshot.Add(conn);
        Debug.Log($"[Server] Client {conn.connectionId} is ready, will receive full snapshot");
      }

      // Send initial hand state so client display matches server-held deck
      if (players.TryGetValue(conn, out var playerState))
      {
        var deck = playerState.Deck;
        conn.Send(new HandStateMessage
        {
          Card0 = deck.Hand[0],
          Card1 = deck.Hand[1],
          Card2 = deck.Hand[2],
          Card3 = deck.Hand[3],
          NextCardId = deck.NextCardId
        });
      }
    }

    private void InitializeMatch()
    {
      var board = matchManager.BoardManager;

      // Team 1 (bottom)
      board.PlaceBuilding(gameplayDirector.SpawnEntity(CardId.KingTower, new System.Numerics.Vector2(0f, -13f), EntityTeam.Team1, true));
      board.PlaceBuilding(gameplayDirector.SpawnEntity(CardId.PrincessTower, new System.Numerics.Vector2(-5.5f, -10.5f), EntityTeam.Team1, true));
      board.PlaceBuilding(gameplayDirector.SpawnEntity(CardId.PrincessTower, new System.Numerics.Vector2(5.5f, -10.5f), EntityTeam.Team1, true));

      // Team 2 (top)
      board.PlaceBuilding(gameplayDirector.SpawnEntity(CardId.KingTower, new System.Numerics.Vector2(0f, 13f), EntityTeam.Team2, true));
      board.PlaceBuilding(gameplayDirector.SpawnEntity(CardId.PrincessTower, new System.Numerics.Vector2(-5.5f, 10.5f), EntityTeam.Team2, true));
      board.PlaceBuilding(gameplayDirector.SpawnEntity(CardId.PrincessTower, new System.Numerics.Vector2(5.5f, 10.5f), EntityTeam.Team2, true));

      gameplayDirector.ResetSnapshotTracking();

      Debug.Log("[Server] Match initialized with king towers and arena towers");
    }

    [ServerCallback]
    private void Update()
    {
      if (matchManager != null && matchManager.IsMatchOver)
        return;

      tickTimer += Time.deltaTime;
      timeAccumulator += Time.deltaTime;
      while (timeAccumulator >= ServerTickSettings.FixedDeltaTime)
      {
        timeAccumulator -= ServerTickSettings.FixedDeltaTime;
        AdvanceTick();
      }

      snapshotTimer += Time.deltaTime;
      if (snapshotTimer >= SNAPSHOT_RATE)
      {
        snapshotTimer -= SNAPSHOT_RATE;
        SyncStateToClients();
      }
    }

    private void AdvanceTick()
    {
      currentTick++;

      foreach (var playerState in players.Values)
      {
        playerState.ElixirState.TickRegen();
      }

      matchManager.ProcessCommandsForTick(currentTick, gameplayDirector);

      gameplayDirector.Update();

      var aiCommand = matchManager.UpdateAI(currentTick + 1);
      if (aiCommand.HasValue)
      {
        var cmd = aiCommand.Value;

        if (enableReplay && replayRecorder != null && replayRecorder.IsRecording)
        {
          replayRecorder.RecordCommand(cmd);
        }

        matchManager.QueueCommand(cmd);
      }

      matchManager.UpdateMatchState(gameplayDirector);

      if (enableDriftDetection && driftDetector != null)
      {
        driftDetector.RecordIfNeeded(currentTick, gameplayDirector);
      }

      if (matchManager.IsMatchOver)
      {
        HandleMatchEnd();
      }
    }

    private void HandleMatchEnd()
    {
      if (matchManager.Winner.HasValue)
      {
        BroadcastMatchEnded(matchManager.Winner.Value);
      }

      if (enableReplay && replayRecorder != null && replayRecorder.IsRecording)
      {
        if (enableDriftDetection && driftDetector != null)
        {
          foreach (var (tick, hash) in driftDetector.Hashes)
          {
            replayRecorder.RecordStateHash(tick, hash);
          }
        }

        replayRecorder.StopRecording(matchManager.Winner, currentTick);

        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filename = $"match_{timestamp}.replay";
        string fullPath = System.IO.Path.Combine(replayFolderPath, filename);

        replayRecorder.SaveToFile(fullPath);
        Debug.Log($"[Server] Replay saved to: {fullPath}");
      }
    }

    public void Server_PlayCard(
        NetworkConnectionToClient sender,
        CardId cardId,
        System.Numerics.Vector2 position)
    {
      if (!players.ContainsKey(sender))
      {
        Debug.LogWarning($"[Server] Unknown player tried to play card");
        return;
      }

      PlayerState playerState = players[sender];

      // Anti-cheat: verify the card exists in the player's current hand
      if (!playerState.Deck.IsInHand(cardId))
      {
        Debug.LogWarning($"[Server] Card {cardId} not in hand");
        SendPlayCardFailed(sender, "Card not in hand");
        return;
      }

      // Validation BEFORE creating command
      if (!playerState.CanAffordCard(cardId))
      {
        Debug.Log($"[Server] Player cannot afford card {cardId}");
        SendPlayCardFailed(sender, "Not enough elixir");
        return;
      }

      if (!IsValidSpawnPosition(position, playerState.Team))
      {
        Debug.LogWarning($"[Server] Invalid spawn position {position}");
        SendPlayCardFailed(sender, "Invalid position");
        return;
      }

      // Create command AFTER validation
      // Queue for NEXT tick since current tick already processed commands
      int playerId = playerState.Team == EntityTeam.Team1 ? 0 : 1;
      int executionTick = currentTick + 1;
      var command = MatchCommand.PlayCard(executionTick, playerId, cardId, position);

      if (enableReplay && replayRecorder != null && replayRecorder.IsRecording)
      {
        replayRecorder.RecordCommand(command);
      }

      matchManager.QueueCommand(command);

      playerState.SpendElixir(cardId);
      playerState.Deck.TryPlay(cardId, out CardId drawnCardId);
      sender.Send(new CardDrawnMessage
      {
        PlayedCardId = cardId,
        NewCardId = drawnCardId,
        NextCardId = playerState.Deck.NextCardId
      });

      Debug.Log($"[Server] Player {playerId} queued PlayCard for tick {executionTick}: card={cardId} at {position}");
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

      // Send each player their current elixir
      foreach (var kvp in players)
      {
        kvp.Key.Send(new ElixirUpdateMessage { MilliElixir = kvp.Value.ElixirState.Current });
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
      players.Remove(conn);
      clientsNeedingFullSnapshot.Remove(conn);
    }
  }
}
