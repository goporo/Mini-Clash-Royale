using System;
using System.Collections.Generic;
using System.Linq;

namespace ClashServer
{
  /// <summary>
  /// Replays a match from a command log in a deterministic way.
  /// No networking, no clients - just pure simulation.
  /// </summary>
  public class ReplayRunner
  {
    private GameplayDirector director;
    private MatchManager matchManager;
    private CommandLog commandLog;
    private int currentTick = 0;
    private ILogger logger;

    // Drift detection
    private List<(int tick, int hash)> recordedHashes;
    private List<(int tick, int hash)> replayHashes = new List<(int, int)>();
    private int firstDriftTick = -1;

    public int CurrentTick => currentTick;
    public bool HasDrift => firstDriftTick >= 0;
    public int FirstDriftTick => firstDriftTick;

    public ReplayRunner(ILogger logger = null)
    {
      this.logger = logger ?? new ConsoleLogger();
    }

    /// <summary>
    /// Run a replay from a ReplayData object.
    /// Returns a ReplayResult with match outcome and any detected drift.
    /// </summary>
    public ReplayResult RunReplay(ReplayData replayData)
    {
      logger.Log($"[Replay] Starting replay: {replayData.Metadata.MatchId}");
      logger.Log($"[Replay] Expected winner: {replayData.Metadata.Winner}, Final tick: {replayData.Metadata.FinalTick}");

      // Setup
      commandLog = new CommandLog();
      foreach (var cmd in replayData.Commands)
      {
        commandLog.AddCommand(cmd);
      }
      recordedHashes = replayData.StateHashes;
      replayHashes.Clear();
      firstDriftTick = -1;

      // Initialize match with same seed for deterministic behavior
      director = new GameplayDirector(logger);
      matchManager = new MatchManager(logger, randomSeed: 12345);
      InitializeMatch();
      currentTick = 0;

      // Run simulation
      int maxTick = replayData.Metadata.FinalTick + 100; // Safety limit
      while (!matchManager.IsMatchOver && currentTick < maxTick)
      {
        AdvanceTick();
      }

      // Create result
      var result = new ReplayResult
      {
        MatchId = replayData.Metadata.MatchId,
        ReplayedTicks = currentTick,
        ExpectedTicks = replayData.Metadata.FinalTick,
        Winner = matchManager.Winner,
        ExpectedWinner = replayData.Metadata.Winner,
        HasDrift = HasDrift,
        FirstDriftTick = firstDriftTick,
        ReplayHashes = new List<(int, int)>(replayHashes)
      };

      LogReplayResult(result);
      return result;
    }

    /// <summary>
    /// Run a replay directly from a command log (for testing).
    /// </summary>
    public ReplayResult RunReplayFromLog(CommandLog log, int expectedFinalTick, EntityTeam? expectedWinner = null)
    {
      var replayData = new ReplayData
      {
        Metadata = new MatchMetadata
        {
          FinalTick = expectedFinalTick,
          Winner = expectedWinner
        },
        Commands = log.GetAllCommands(),
        StateHashes = new List<(int, int)>()
      };

      return RunReplay(replayData);
    }

    private void InitializeMatch()
    {
      // Team 1 (bottom)
      director.SpawnEntity("kingtower", new System.Numerics.Vector2(0, -6), EntityTeam.Team1, true);
      director.SpawnEntity("tower", new System.Numerics.Vector2(-5, -3), EntityTeam.Team1, true);
      director.SpawnEntity("tower", new System.Numerics.Vector2(5, -3), EntityTeam.Team1, true);

      // Team 2 (top)
      director.SpawnEntity("kingtower", new System.Numerics.Vector2(0, 18), EntityTeam.Team2, true);
      director.SpawnEntity("tower", new System.Numerics.Vector2(-5, 15), EntityTeam.Team2, true);
      director.SpawnEntity("tower", new System.Numerics.Vector2(5, 15), EntityTeam.Team2, true);

      logger.Log("[Replay] Match initialized");
    }

    private void AdvanceTick()
    {
      // Increment tick FIRST (same as server)
      currentTick++;

      // Apply commands for this tick (includes both player and AI commands)
      var commands = commandLog.GetCommandsForTick(currentTick);
      foreach (var cmd in commands)
      {
        ApplyCommand(cmd);
      }

      // Update simulation
      director.Update();
      matchManager.UpdateMatchState(director);

      // Drift detection
      DetectDrift();
    }

    private void ApplyCommand(MatchCommand cmd)
    {
      switch (cmd.Type)
      {
        case CommandType.PlayCard:
          ApplyPlayCard(cmd);
          break;
        case CommandType.Surrender:
          ApplySurrender(cmd);
          break;
        case CommandType.Emote:
          // Emotes don't affect game state
          break;
      }
    }

    private void ApplyPlayCard(MatchCommand cmd)
    {
      // Determine team based on player ID (simple mapping)
      EntityTeam team = cmd.PlayerId == 0 ? EntityTeam.Team1 : EntityTeam.Team2;

      // Map card ID to entity type (simplified)
      string entityType = MapCardIdToType(cmd.CardId);

      director.SpawnEntity(entityType, cmd.Position, team);
      logger.Log($"[Replay T{currentTick}] Player {cmd.PlayerId} played {entityType} at {cmd.Position}");
    }

    private void ApplySurrender(MatchCommand cmd)
    {
      logger.Log($"[Replay T{currentTick}] Player {cmd.PlayerId} surrendered");
      // Implement surrender logic if needed
    }

    private string MapCardIdToType(int cardId)
    {
      // Simple mapping - extend as needed
      return cardId switch
      {
        0 => "knight",
        1 => "archer",
        2 => "giant",
        _ => "knight"
      };
    }

    private void DetectDrift()
    {
      // Only check on ticks where we have recorded hashes
      var recordedHash = recordedHashes.FirstOrDefault(h => h.tick == currentTick);
      if (recordedHash == default)
        return;

      int currentHash = HashBoardState();
      replayHashes.Add((currentTick, currentHash));

      if (currentHash != recordedHash.hash && firstDriftTick < 0)
      {
        firstDriftTick = currentTick;
        logger.Log($"[Replay] DRIFT DETECTED at tick {currentTick}!");
        logger.Log($"[Replay] Expected hash: {recordedHash.hash}, Got: {currentHash}");
      }
    }

    private int HashBoardState()
    {
      // Simple deterministic hash of game state
      // MUST match DriftDetector.HashBoardState() exactly
      int hash = 17;

      // Hash tick count
      hash = hash * 31 + (int)director.CurrentTick;

      var entities = director.GetEntities()
          .Where(e => e.IsAlive)
          .OrderBy(e => e.Id)
          .ToList();

      foreach (var entity in entities)
      {
        hash = hash * 31 + entity.Id;
        hash = hash * 31 + entity.Type.GetHashCode();
        hash = hash * 31 + entity.Team.GetHashCode();
        hash = hash * 31 + (int)(entity.Position.X * 100);
        hash = hash * 31 + (int)(entity.Position.Y * 100);
        hash = hash * 31 + (int)(entity.Stats.CurrentHP * 10);
        hash = hash * 31 + entity.AttackCooldownTicks;
        hash = hash * 31 + (entity.Target?.Id ?? -1);
        hash = hash * 31 + (entity.IsBuilding ? 1 : 0);
      }

      return hash;
    }

    private void LogReplayResult(ReplayResult result)
    {
      logger.Log("=== REPLAY RESULT ===");
      logger.Log($"Match ID: {result.MatchId}");
      logger.Log($"Winner: {result.Winner} (Expected: {result.ExpectedWinner})");
      logger.Log($"Ticks: {result.ReplayedTicks} (Expected: {result.ExpectedTicks})");
      logger.Log($"Match: {(result.IsMatchCorrect ? "CORRECT" : "MISMATCH")}");
      logger.Log($"Drift: {(result.HasDrift ? $"YES at tick {result.FirstDriftTick}" : "NO")}");
      logger.Log("=====================");
    }
  }

  /// <summary>
  /// Result of a replay run.
  /// </summary>
  public class ReplayResult
  {
    public string MatchId;
    public int ReplayedTicks;
    public int ExpectedTicks;
    public EntityTeam? Winner;
    public EntityTeam? ExpectedWinner;
    public bool HasDrift;
    public int FirstDriftTick;
    public List<(int tick, int hash)> ReplayHashes;

    public bool IsMatchCorrect =>
        Winner == ExpectedWinner &&
        Math.Abs(ReplayedTicks - ExpectedTicks) < 10; // Allow small tick difference

    public bool IsPerfect =>
        IsMatchCorrect && !HasDrift;

    public override string ToString()
    {
      string status = IsPerfect ? "PERFECT" :
                      IsMatchCorrect ? "CORRECT (with drift)" : "FAILED";
      return $"Replay [{status}]: Winner={Winner}, Ticks={ReplayedTicks}, Drift={HasDrift}";
    }
  }
}
