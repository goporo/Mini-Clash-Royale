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

    // Replay system
    private ReplayRecorder replayRecorder;
    private DriftDetector driftDetector;
    [SerializeField] private bool enableReplay = true;
    [SerializeField] private bool enableDriftDetection = true;
    [SerializeField] private string replayFolderPath = "Replays";

    public const float FIXED_DT = 0.1f; // Callbacks every 0.1s
    private const float SNAPSHOT_RATE = 0.1f;
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
      players = new Dictionary<NetworkConnectionToClient, PlayerState>();
      clientsNeedingFullSnapshot = new HashSet<NetworkConnectionToClient>();

      // Initialize replay system
      if (enableReplay)
      {
        replayRecorder = new ReplayRecorder();

        // Create replay folder if it doesn't exist
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
      // Stop updates when match is over
      if (matchManager != null && matchManager.IsMatchOver)
        return;

      tickTimer += Time.deltaTime;
      timeAccumulator += Time.deltaTime;
      while (timeAccumulator >= FIXED_DT)
      {
        timeAccumulator -= FIXED_DT;
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

      // Process queued commands for this tick BEFORE simulation
      matchManager.ProcessCommandsForTick(currentTick, gameplayDirector);

      // Run simulation
      gameplayDirector.Update();

      // Update AI and record any commands
      var aiCommand = matchManager.UpdateAI(currentTick + 1);
      if (aiCommand.HasValue)
      {
        var cmd = aiCommand.Value;

        // Record AI command for replay
        if (enableReplay && replayRecorder != null && replayRecorder.IsRecording)
        {
          replayRecorder.RecordCommand(cmd);
        }

        // Queue AI command
        matchManager.QueueCommand(cmd);
        Debug.Log($"[Server] AI queued command for tick {cmd.Tick}: {cmd.Type}");
      }

      matchManager.UpdateMatchState(gameplayDirector);

      // Record state hash for drift detection
      if (enableDriftDetection && driftDetector != null)
      {
        driftDetector.RecordIfNeeded(currentTick, gameplayDirector);
      }

      // Check for match end
      if (matchManager.IsMatchOver)
      {
        HandleMatchEnd();
      }
    }

    private void HandleMatchEnd()
    {
      // Broadcast match ended (handle draw case)
      if (matchManager.Winner.HasValue)
      {
        BroadcastMatchEnded(matchManager.Winner.Value);
      }

      // Stop replay recording and save
      if (enableReplay && replayRecorder != null && replayRecorder.IsRecording)
      {
        // Add drift detection hashes
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

      // Validation BEFORE creating command
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

      // Create command AFTER validation
      // Queue for NEXT tick since current tick already processed commands
      int playerId = playerState.Team == EntityTeam.Team1 ? 0 : 1;
      int executionTick = currentTick + 1;
      var command = MatchCommand.PlayCard(executionTick, playerId, cardId, position);

      // Log command for replay
      if (enableReplay && replayRecorder != null && replayRecorder.IsRecording)
      {
        replayRecorder.RecordCommand(command);
      }

      // Queue command for execution (will execute at next tick)
      matchManager.QueueCommand(command);

      // Spend elixir
      playerState.SpendElixir(cardId);

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
