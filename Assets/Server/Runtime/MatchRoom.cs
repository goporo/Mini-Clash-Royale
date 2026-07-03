using Mirror;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ClashShared;

namespace ClashServer
{
  public class MatchRoom
  {
    public string MatchId { get; }

    readonly GameplayDirector gameplayDirector;
    readonly MatchManager matchManager;
    readonly SimpleMatchAi aiController;
    readonly MatchMode matchMode;
    readonly bool enableReplay;
    readonly bool enableDriftDetection;
    readonly string replayFolderPath;

    readonly Dictionary<NetworkConnectionToClient, PlayerState> players = new();
    readonly HashSet<NetworkConnectionToClient> clientsNeedingFullSnapshot = new();

    ReplayRecorder replayRecorder;
    DriftDetector driftDetector;

    bool matchStarted = false;
    bool matchEndHandled = false;
    int currentTick = 0;
    float timeAccumulator = 0f;
    float snapshotTimer = 0f;

    const float SNAPSHOT_RATE = ServerTickSettings.FixedDeltaTime;

    public bool IsStarted => matchStarted;
    public bool IsOver => matchManager.IsMatchOver;

    public MatchRoom(string matchId, MatchMode mode, bool enableReplay, bool enableDriftDetection, string replayFolderPath)
    {
      MatchId = matchId;
      matchMode = mode;
      this.enableReplay = enableReplay;
      this.enableDriftDetection = enableDriftDetection;
      this.replayFolderPath = replayFolderPath;

      var logger = new UnityLogger();
      var boardManager = new BoardManager();
      gameplayDirector = new GameplayDirector(boardManager, logger);
      matchManager = new MatchManager(boardManager, logger);
      aiController = new SimpleMatchAi(logger);
      matchManager.SpellCastResolved += HandleSpellCastResolved;

      if (enableReplay)
      {
        replayRecorder = new ReplayRecorder();
        if (!System.IO.Directory.Exists(replayFolderPath))
          System.IO.Directory.CreateDirectory(replayFolderPath);
        var metadata = new MatchMetadata("Player1", mode == MatchMode.PvP ? "Player2" : "AI");
        replayRecorder.StartRecording(metadata);
      }

      if (enableDriftDetection)
        driftDetector = new DriftDetector(hashInterval: 10, logger: logger);

      MatchSetup.InitializeStandardArena(gameplayDirector, matchManager.BoardManager);
      gameplayDirector.ResetSnapshotTracking();

      Debug.Log($"[Room {matchId}] Created — waiting for players");
    }

    // Trả về false nếu room đã đủ 2 người
    public bool TryAddPlayer(NetworkConnectionToClient conn, List<CardId> deck, EntityTeam team)
    {
      if (players.Count >= 2)
      {
        Debug.LogWarning($"[Room {MatchId}] TryAddPlayer rejected conn {conn.connectionId} — room already full");
        return false;
      }
      if (players.Values.Any(p => p.Team == team))
      {
        Debug.LogWarning($"[Room {MatchId}] TryAddPlayer rejected conn {conn.connectionId} — team {team} already taken");
        return false;
      }

      var playerState = new PlayerState(conn, team, deck);
      players[conn] = playerState;

      Debug.Log($"[Room {MatchId}] Player {conn.connectionId} joined as {team} ({players.Count}/2)");
      return true;
    }

    public void HandleClientReady(NetworkConnectionToClient conn)
    {
      if (!players.TryGetValue(conn, out var playerState))
      {
        Debug.LogWarning($"[Room {MatchId}] HandleClientReady — conn {conn.connectionId} not in players, ignoring");
        return;
      }

      clientsNeedingFullSnapshot.Add(conn);

      bool shouldStart = matchMode == MatchMode.PvE
        ? players.Count >= 1
        : players.Count == 2 && clientsNeedingFullSnapshot.Count == 2;

      Debug.Log($"[Room {MatchId}] Ready received from conn {conn.connectionId} ({playerState.Team}) — players={players.Count}/2, readyCount={clientsNeedingFullSnapshot.Count}, shouldStart={shouldStart}");

      if (shouldStart && !matchStarted)
      {
        matchStarted = true;
        Debug.Log($"[Room {MatchId}] {(matchMode == MatchMode.PvE ? "PvE" : "Both players")} ready — match started!");
        SendHandStateToAll();
        BroadcastToRoom(new MatchReadyMessage());
      }
    }

    void SendHandStateToAll()
    {
      foreach (var kvp in players)
      {
        var deck = kvp.Value.Deck;
        kvp.Key.Send(new HandStateMessage
        {
          Card0 = deck.Hand[0],
          Card1 = deck.Hand[1],
          Card2 = deck.Hand[2],
          Card3 = deck.Hand[3],
          NextCardId = deck.NextCardId,
          LocalTeam = kvp.Value.Team
        });
      }
    }

    public void Tick(float deltaTime)
    {
      if (!matchStarted || matchManager.IsMatchOver) return;

      timeAccumulator += deltaTime;
      while (timeAccumulator >= ServerTickSettings.FixedDeltaTime)
      {
        timeAccumulator -= ServerTickSettings.FixedDeltaTime;
        AdvanceTick();
      }

      snapshotTimer += deltaTime;
      if (snapshotTimer >= SNAPSHOT_RATE)
      {
        snapshotTimer -= SNAPSHOT_RATE;
        SyncStateToClients();
      }
    }

    public void HandlePlayCard(NetworkConnectionToClient sender, uint requestId, CardId cardId, System.Numerics.Vector2 position)
    {
      if (!players.TryGetValue(sender, out var playerState))
      {
        sender.Send(new PlayCardFailedMessage { Reason = "Not in this match" });
        return;
      }

      if (matchManager.IsMatchOver)
      {
        sender.Send(new PlayCardFailedMessage { Reason = "Match already ended" });
        return;
      }

      if (!playerState.TryRegisterPlayRequest(requestId, out string reqFail))
      {
        sender.Send(new PlayCardFailedMessage { Reason = reqFail });
        return;
      }

      if (!playerState.Deck.IsInHand(cardId))
      {
        sender.Send(new PlayCardFailedMessage { Reason = "Card not in hand" });
        return;
      }

      if (!playerState.CanAffordCard(cardId))
      {
        sender.Send(new PlayCardFailedMessage { Reason = "Not enough elixir" });
        return;
      }

      if (!matchManager.TryValidatePlayCard(cardId, position, playerState.Team, gameplayDirector, out string valFail))
      {
        sender.Send(new PlayCardFailedMessage { Reason = valFail });
        return;
      }

      int playerId = playerState.Team == EntityTeam.Team1 ? 0 : 1;
      int executionTick = currentTick + 1;
      var command = MatchCommand.PlayCard(executionTick, playerId, cardId, position);

      if (!playerState.TrySpendElixir(cardId)) { sender.Send(new PlayCardFailedMessage { Reason = "Not enough elixir" }); return; }
      if (!playerState.TryPlayCardFromHand(cardId, out CardId drawnCardId)) { sender.Send(new PlayCardFailedMessage { Reason = "Card not in hand" }); return; }

      if (enableReplay && replayRecorder?.IsRecording == true)
        replayRecorder.RecordCommand(command);

      matchManager.QueueCommand(command);
      sender.Send(new CardDrawnMessage { PlayedCardId = cardId, NewCardId = drawnCardId, NextCardId = playerState.Deck.NextCardId });
    }

    // Trả về true nếu room không còn ai — gọi ý cho caller dọn khỏi MatchRegistry
    public bool HandlePlayerDisconnected(NetworkConnectionToClient conn)
    {
      if (players.TryGetValue(conn, out var state))
        Debug.Log($"[Room {MatchId}] Player {conn.connectionId} ({state.Team}) disconnected");

      players.Remove(conn);
      clientsNeedingFullSnapshot.Remove(conn);

      return players.Count == 0;
    }

    public bool HasPlayer(NetworkConnectionToClient conn) => players.ContainsKey(conn);
    public bool HasBothPlayers() => players.Count == 2;

    public void Destroy()
    {
      matchManager.SpellCastResolved -= HandleSpellCastResolved;
      foreach (var conn in players.Keys.ToList())
        conn.Disconnect();
      Debug.Log($"[Room {MatchId}] Destroyed");
    }

    void AdvanceTick()
    {
      currentTick++;

      foreach (var p in players.Values)
        p.ElixirState.TickRegen();

      if (IsAiActive()) aiController?.TickRegen();

      matchManager.ProcessCommandsForTick(currentTick, gameplayDirector);
      gameplayDirector.Update();

      var aiCommand = IsAiActive() ? aiController?.TryCreateCommand(currentTick + 1, gameplayDirector, matchManager) : null;
      if (aiCommand.HasValue)
      {
        if (enableReplay && replayRecorder?.IsRecording == true)
          replayRecorder.RecordCommand(aiCommand.Value);
        matchManager.QueueCommand(aiCommand.Value);
      }

      matchManager.UpdateMatchState(gameplayDirector);

      if (enableDriftDetection && driftDetector != null)
        driftDetector.RecordIfNeeded(currentTick, gameplayDirector);

      if (matchManager.IsMatchOver)
        HandleMatchEnd();
    }

    void HandleMatchEnd()
    {
      if (matchEndHandled) return;
      matchEndHandled = true;

      if (matchManager.Winner.HasValue)
      {
        BroadcastToRoom(new MatchEndedMessage { Winner = matchManager.Winner.Value });
        Debug.Log($"[Room {MatchId}] Match ended — Winner: {matchManager.Winner.Value} at tick {currentTick}");
      }

      if (enableReplay && replayRecorder?.IsRecording == true)
      {
        if (enableDriftDetection && driftDetector != null)
          foreach (var (tick, hash) in driftDetector.Hashes)
            replayRecorder.RecordStateHash(tick, hash);

        replayRecorder.StopRecording(matchManager.Winner, currentTick);
        string filename = $"match_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.replay";
        replayRecorder.SaveToFile(System.IO.Path.Combine(replayFolderPath, filename));
      }
    }

    void SyncStateToClients()
    {
      foreach (var conn in clientsNeedingFullSnapshot.ToList())
      {
        var snapshot = gameplayDirector.GenerateFullSnapshot();
        conn.Send(new FullSnapshotMessage { Snapshot = snapshot });
        clientsNeedingFullSnapshot.Remove(conn);
      }

      foreach (var kvp in players)
        kvp.Key.Send(new ElixirUpdateMessage { MilliElixir = kvp.Value.ElixirState.Current });

      var delta = gameplayDirector.GenerateDeltaSnapshot();
      if (delta.SpawnedEntities.Count > 0 || delta.DestroyedEntityIds.Count > 0 || delta.UpdatedEntities.Count > 0)
        BroadcastToRoom(new DeltaSnapshotMessage { Delta = delta });
    }

    void BroadcastToRoom<T>(T msg) where T : struct, NetworkMessage
    {
      foreach (var conn in players.Keys)
        conn.Send(msg);
    }

    void HandleSpellCastResolved(CardId cardId, System.Numerics.Vector2 position, EntityTeam team)
    {
      BroadcastToRoom(new SpellCastMessage
      {
        CardId = cardId,
        Position = Vector2Data.FromVector2(position),
        Team = team
      });
    }

    bool IsAiActive()
    {
      if (matchMode == MatchMode.PvP) return false;
      bool hasTeam1 = players.Values.Any(p => p.Team == EntityTeam.Team1);
      bool hasTeam2 = players.Values.Any(p => p.Team == EntityTeam.Team2);
      return hasTeam1 && !hasTeam2;
    }
  }
}
