using System.Collections.Generic;
using System.Linq;

namespace ClashServer
{
  /// <summary>
  /// Debug-only utility for detecting simulation drift.
  /// Records hashes of board state at intervals and compares during replay.
  /// This is CRITICAL for debugging determinism issues.
  /// </summary>
  public class DriftDetector
  {
    private int hashInterval;
    private List<(int tick, int hash)> hashes = new List<(int, int)>();
    private ILogger logger;

    public List<(int tick, int hash)> Hashes => new List<(int, int)>(hashes);

    /// <summary>
    /// Create a drift detector.
    /// </summary>
    /// <param name="hashInterval">Record a hash every N ticks. Recommended: 10-30</param>
    /// <param name="logger">Logger for output</param>
    public DriftDetector(int hashInterval = 10, ILogger logger = null)
    {
      this.hashInterval = hashInterval;
      this.logger = logger ?? new ConsoleLogger();
    }

    /// <summary>
    /// Record a state hash if this tick is a hash checkpoint.
    /// Call this every tick.
    /// </summary>
    public void RecordIfNeeded(int tick, GameplayDirector director)
    {
      if (tick % hashInterval != 0)
        return;

      int hash = HashBoardState(director);
      hashes.Add((tick, hash));
      logger.Log($"[DriftDetector] T{tick}: Hash={hash}");
    }

    /// <summary>
    /// Clear all recorded hashes.
    /// </summary>
    public void Clear()
    {
      hashes.Clear();
    }

    /// <summary>
    /// Generate a deterministic hash of the current board state.
    /// CRITICAL: This must be 100% deterministic across runs.
    /// </summary>
    private int HashBoardState(GameplayDirector director)
    {
      int hash = 17;

      // Hash tick count
      hash = hash * 31 + (int)director.CurrentTick;

      // Get all alive entities in a deterministic order
      var entities = director.GetEntities()
          .Where(e => e.IsAlive)
          .OrderBy(e => e.Id)
          .ToList();

      foreach (var entity in entities)
      {
        // Hash entity properties that matter for gameplay
        hash = hash * 31 + entity.Id;
        hash = hash * 31 + entity.Type.GetHashCode();
        hash = hash * 31 + entity.Team.GetHashCode();

        // Position (rounded to avoid floating point drift)
        hash = hash * 31 + (int)(entity.Position.X * 100);
        hash = hash * 31 + (int)(entity.Position.Y * 100);

        // Combat state
        hash = hash * 31 + (int)(entity.Stats.CurrentHP * 10);
        hash = hash * 31 + entity.AttackCooldownTicks;
        hash = hash * 31 + (entity.Target?.Id ?? -1);

        // Building flag
        hash = hash * 31 + (entity.IsBuilding ? 1 : 0);
      }

      return hash;
    }

    /// <summary>
    /// Compare recorded hashes with a set of expected hashes.
    /// Returns the first tick where drift occurred, or -1 if no drift.
    /// </summary>
    public int CompareTo(List<(int tick, int hash)> expectedHashes)
    {
      var expectedDict = expectedHashes.ToDictionary(h => h.tick, h => h.hash);

      foreach (var (tick, hash) in hashes)
      {
        if (expectedDict.TryGetValue(tick, out int expectedHash))
        {
          if (hash != expectedHash)
          {
            logger.Log($"[DriftDetector] DRIFT at T{tick}: Expected {expectedHash}, Got {hash}");
            return tick;
          }
        }
      }

      return -1;
    }

    /// <summary>
    /// Create a detailed diff report between expected and actual state.
    /// Useful for debugging exactly what changed.
    /// </summary>
    public string GenerateDriftReport(List<(int tick, int hash)> expectedHashes)
    {
      var report = new System.Text.StringBuilder();
      report.AppendLine("=== DRIFT REPORT ===");

      var expectedDict = expectedHashes.ToDictionary(h => h.tick, h => h.hash);

      foreach (var (tick, hash) in hashes)
      {
        if (expectedDict.TryGetValue(tick, out int expectedHash))
        {
          if (hash == expectedHash)
          {
            report.AppendLine($"T{tick}: OK (Hash={hash})");
          }
          else
          {
            report.AppendLine($"T{tick}: MISMATCH (Expected={expectedHash}, Got={hash})");
          }
        }
        else
        {
          report.AppendLine($"T{tick}: MISSING in expected (Hash={hash})");
        }
      }

      report.AppendLine("====================");
      return report.ToString();
    }
  }
}
