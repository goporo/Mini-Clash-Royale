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
  public class ServerMatchController : NetworkBehaviour
  {
    public static ServerMatchController Instance;

    private GameplayDirector gameplayDirector;
    private MatchManager matchManager;
    private SimpleMatchAi aiController;
    private Dictionary<NetworkConnectionToClient, PlayerState> players;
    private MatchMode matchMode = MatchMode.PvE;

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

    public void SetMatchMode(MatchMode mode)
    {
      matchMode = mode;
    }

    public override void OnStartServer()
    {
      Debug.Log("[Server] ServerMatchController started");
      Instance = this;

      if (NetworkManager.singleton is MyNetworkManager nm)
        matchMode = nm.MatchMode;

      Debug.Log($"[Server] Match mode: {matchMode}");

      NetworkServer.OnConnectedEvent += HandlePlayerConnected;
      NetworkServer.OnDisconnectedEvent += HandlePlayerDisconnected;

      players = new Dictionary<NetworkConnectionToClient, PlayerState>();
      clientsNeedingFullSnapshot = new HashSet<NetworkConnectionToClient>();

      ResetMatch();
    }

    private void ResetMatch()
    {
      if (matchManager != null)
        matchManager.SpellCastResolved -= HandleSpellCastResolved;

      var logger = new UnityLogger();
      var boardManager = new BoardManager();
      gameplayDirector = new GameplayDirector(boardManager, logger);
      matchManager = new MatchManager(boardManager, logger);
      aiController = new SimpleMatchAi(logger);
      matchManager.SpellCastResolved += HandleSpellCastResolved;

      currentTick = 0;
      tickTimer = 0;
      snapshotTimer = 0;
      timeAccumulator = 0;
      clientsNeedingFullSnapshot.Clear();

      if (enableReplay)
      {
        replayRecorder = new ReplayRecorder();

        if (!System.IO.Directory.Exists(replayFolderPath))
          System.IO.Directory.CreateDirectory(replayFolderPath);

        var metadata = new MatchMetadata("Player1", matchMode == MatchMode.PvP ? "Player2" : "AI");
        replayRecorder.StartRecording(metadata);
      }

      if (enableDriftDetection)
      {
        driftDetector = new DriftDetector(hashInterval: 10, logger: logger);
      }

      InitializeMatch();
      Debug.Log("[Server] Match reset — waiting for players");
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
          NextCardId = deck.NextCardId,
          LocalTeam = playerState.Team
        });
      }
    }

    private void InitializeMatch()
    {
      MatchSetup.InitializeStandardArena(gameplayDirector, matchManager.BoardManager);
      gameplayDirector.ResetSnapshotTracking();

      Debug.Log("[Server] Match initialized with king towers and arena towers");
    }

    [ServerCallback]
    private void Update()
    {
      if (players == null || gameplayDirector == null || matchManager == null)
        return;

      if (matchManager.IsMatchOver)
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

      if (IsAiActive())
        aiController?.TickRegen();

      matchManager.ProcessCommandsForTick(currentTick, gameplayDirector);

      gameplayDirector.Update();

      var aiCommand = IsAiActive()
        ? aiController?.TryCreateCommand(currentTick + 1, gameplayDirector, matchManager)
        : null;
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

    private bool matchEndHandled = false;

    private void HandleMatchEnd()
    {
      if (matchEndHandled) return;
      matchEndHandled = true;

      if (matchManager.Winner.HasValue)
      {
        BroadcastMatchEnded(matchManager.Winner.Value);
        Debug.Log($"[Server] *** Match ended — Winner: {matchManager.Winner.Value} at tick {currentTick} ***");
      }

      if (enableReplay && replayRecorder != null && replayRecorder.IsRecording)
      {
        if (enableDriftDetection && driftDetector != null)
        {
          foreach (var (tick, hash) in driftDetector.Hashes)
            replayRecorder.RecordStateHash(tick, hash);
        }

        replayRecorder.StopRecording(matchManager.Winner, currentTick);

        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filename = $"match_{timestamp}.replay";
        string fullPath = System.IO.Path.Combine(replayFolderPath, filename);

        replayRecorder.SaveToFile(fullPath);
        Debug.Log($"[Server] Replay saved to: {fullPath}");
      }

      Debug.Log("[Server] Resetting in 5 seconds...");
      Invoke(nameof(RestartMatch), 5f);
    }

    private void RestartMatch()
    {
      Debug.Log("[Server] Disconnecting players and resetting match...");

      foreach (var conn in players.Keys.ToList())
        conn.Disconnect();

      players.Clear();
      clientsNeedingFullSnapshot.Clear();
      matchEndHandled = false;

      ResetMatch();
      Debug.Log("[Server] Ready — waiting for next 2 players on port 7777");
    }

    public void Server_PlayCard(
        NetworkConnectionToClient sender,
        uint requestId,
        CardId cardId,
        System.Numerics.Vector2 position)
    {
      if (!players.TryGetValue(sender, out PlayerState playerState))
      {
        Debug.LogWarning($"[Server] Unknown player tried to play card");
        return;
      }

      if (matchManager == null || matchManager.IsMatchOver)
      {
        SendPlayCardFailed(sender, "Match already ended");
        return;
      }

      if (playerState.Connection != sender)
      {
        Debug.LogWarning($"[Server] Sender does not own this player state");
        SendPlayCardFailed(sender, "Invalid player ownership");
        return;
      }

      if (!playerState.TryRegisterPlayRequest(requestId, out string requestFailureReason))
      {
        Debug.LogWarning($"[Server] Rejected play request {requestId}: {requestFailureReason}");
        SendPlayCardFailed(sender, requestFailureReason);
        return;
      }

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

      if (!matchManager.TryValidatePlayCard(cardId, position, playerState.Team, out string validationFailureReason))
      {
        Debug.LogWarning($"[Server] Invalid play for request {requestId}: {validationFailureReason}");
        SendPlayCardFailed(sender, validationFailureReason);
        return;
      }

      // Create command AFTER validation
      // Queue for NEXT tick since current tick already processed commands
      int playerId = playerState.Team == EntityTeam.Team1 ? 0 : 1;
      int executionTick = currentTick + 1;
      var command = MatchCommand.PlayCard(executionTick, playerId, cardId, position);

      if (!playerState.TrySpendElixir(cardId))
      {
        Debug.LogWarning($"[Server] Elixir spend failed for request {requestId}");
        SendPlayCardFailed(sender, "Not enough elixir");
        return;
      }

      if (!playerState.TryPlayCardFromHand(cardId, out CardId drawnCardId))
      {
        Debug.LogWarning($"[Server] Hand update failed for request {requestId}");
        SendPlayCardFailed(sender, "Card not in hand");
        return;
      }

      if (enableReplay && replayRecorder != null && replayRecorder.IsRecording)
      {
        replayRecorder.RecordCommand(command);
      }

      matchManager.QueueCommand(command);

      sender.Send(new CardDrawnMessage
      {
        PlayedCardId = cardId,
        NewCardId = drawnCardId,
        NextCardId = playerState.Deck.NextCardId
      });

      Debug.Log($"[Server] Player {playerId} queued PlayCard for tick {executionTick}: card={cardId} at {position}");
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

    private void HandleSpellCastResolved(CardId cardId, System.Numerics.Vector2 position, EntityTeam team)
    {
      NetworkServer.SendToAll(new SpellCastMessage
      {
        CardId = cardId,
        Position = Vector2Data.FromVector2(position),
        Team = team
      });
    }

    private void SendPlayCardFailed(NetworkConnectionToClient conn, string reason)
    {
      conn.Send(new PlayCardFailedMessage { Reason = reason });
    }

    private void BroadcastMatchEnded(EntityTeam winner)
    {
      Debug.Log($"[Server] *** Match ended — Winner: {winner} at tick {currentTick} ***");
      NetworkServer.SendToAll(new MatchEndedMessage { Winner = winner });
    }

    public override void OnStopServer()
    {
      NetworkServer.OnConnectedEvent -= HandlePlayerConnected;
      NetworkServer.OnDisconnectedEvent -= HandlePlayerDisconnected;

      if (matchManager != null)
        matchManager.SpellCastResolved -= HandleSpellCastResolved;

      base.OnStopServer();
    }

    private void HandlePlayerConnected(NetworkConnectionToClient conn)
    {
      if (players.Count >= 2)
      {
        Debug.LogWarning($"[Server] Rejected connection {conn.connectionId} — match already full (2/2)");
        conn.Disconnect();
        return;
      }

      EntityTeam team = players.Count == 0 ? EntityTeam.Team1 : EntityTeam.Team2;
      PlayerState playerState = new PlayerState(conn, team);
      players[conn] = playerState;

      Debug.Log($"[Server] Player {conn.connectionId} joined as {team} | players={players.Count}/2");

      if (players.Count == 2)
        Debug.Log("[Server] >> Both players connected — match starting!");
      else
        Debug.Log("[Server] >> Waiting for 2nd player...");
    }

    private void HandlePlayerDisconnected(NetworkConnectionToClient conn)
    {
      if (players.TryGetValue(conn, out var state))
        Debug.Log($"[Server] Player {conn.connectionId} ({state.Team}) left | players remaining={players.Count - 1}");

      players.Remove(conn);
      clientsNeedingFullSnapshot.Remove(conn);
    }

    private bool IsAiActive()
    {
      if (matchMode == MatchMode.PvP) return false;

      bool hasTeam1Human = players.Values.Any(playerState => playerState.Team == EntityTeam.Team1);
      bool hasTeam2Human = players.Values.Any(playerState => playerState.Team == EntityTeam.Team2);
      return hasTeam1Human && !hasTeam2Human;
    }
  }
}
