using System;
using System.Collections.Generic;
using System.Linq;
using ClashShared;

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
      recordedHashes = replayData.StateHashes;
      replayHashes.Clear();
      firstDriftTick = -1;

      // Initialize match with same seed for deterministic behavior
      var boardManager = new BoardManager();
      director = new GameplayDirector(boardManager, logger);
      matchManager = new MatchManager(boardManager, logger, randomSeed: 12345);
      InitializeMatch();
      foreach (MatchCommand cmd in replayData.Commands)
        matchManager.QueueCommand(cmd);
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
      MatchSetup.InitializeStandardArena(director, matchManager.BoardManager);
      logger.Log("[Replay] Match initialized");
    }

    private void AdvanceTick()
    {
      // Increment tick FIRST (same as server)
      currentTick++;

      matchManager.ProcessCommandsForTick(currentTick, director);
      director.Update();
      matchManager.UpdateMatchState(director);

      // Drift detection
      DetectDrift();
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
