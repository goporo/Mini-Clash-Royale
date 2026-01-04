using UnityEngine;
using System.IO;
using System.Linq;

namespace ClashServer
{
  /// <summary>
  /// Unity editor utility for testing and validating replays.
  /// Add this to a GameObject in the editor to run replay tests.
  /// </summary>
  public class ReplayTester : MonoBehaviour
  {
    [Header("Replay Settings")]
    [Tooltip("Path to the Replays folder")]
    public string replayFolderPath = "Replays";

    [Tooltip("Specific replay file to test (leave empty to test latest)")]
    public string specificReplayFile = "";

    [Header("Test Actions")]
    [Tooltip("Run replay test on start")]
    public bool testOnStart = false;

    [Header("Status")]
    [SerializeField]
    private string lastTestResult = "No test run yet";

    private void Start()
    {
      if (testOnStart)
      {
        TestLatestReplay();
      }
    }

    /// <summary>
    /// Test the latest replay file in the replay folder.
    /// Call this from the Unity Inspector or via code.
    /// </summary>
    [ContextMenu("Test Latest Replay")]
    public void TestLatestReplay()
    {
      if (!Directory.Exists(replayFolderPath))
      {
        lastTestResult = $"Replay folder not found: {replayFolderPath}";
        Debug.LogError(lastTestResult);
        return;
      }

      string replayFile;
      if (!string.IsNullOrEmpty(specificReplayFile))
      {
        replayFile = Path.Combine(replayFolderPath, specificReplayFile);
      }
      else
      {
        var files = Directory.GetFiles(replayFolderPath, "*.replay")
            .OrderByDescending(f => File.GetLastWriteTime(f))
            .ToList();

        if (files.Count == 0)
        {
          lastTestResult = "No replay files found";
          Debug.LogWarning(lastTestResult);
          return;
        }

        replayFile = files[0];
      }

      TestReplay(replayFile);
    }

    /// <summary>
    /// Test all replay files in the folder.
    /// </summary>
    [ContextMenu("Test All Replays")]
    public void TestAllReplays()
    {
      if (!Directory.Exists(replayFolderPath))
      {
        lastTestResult = $"Replay folder not found: {replayFolderPath}";
        Debug.LogError(lastTestResult);
        return;
      }

      var files = Directory.GetFiles(replayFolderPath, "*.replay");
      if (files.Length == 0)
      {
        lastTestResult = "No replay files found";
        Debug.LogWarning(lastTestResult);
        return;
      }

      Debug.Log($"[ReplayTester] Testing {files.Length} replay files...");
      int passed = 0;
      int failed = 0;

      foreach (var file in files)
      {
        ReplayResult result = TestReplay(file);
        if (result != null && result.IsMatchCorrect)
          passed++;
        else
          failed++;
      }

      lastTestResult = $"Tested {files.Length} replays: {passed} passed, {failed} failed";
      Debug.Log($"[ReplayTester] {lastTestResult}");
    }

    private ReplayResult TestReplay(string filePath)
    {
      if (!File.Exists(filePath))
      {
        lastTestResult = $"Replay file not found: {filePath}";
        Debug.LogError(lastTestResult);
        return null;
      }

      Debug.Log($"[ReplayTester] Testing replay: {Path.GetFileName(filePath)}");

      try
      {
        // Load replay
        ReplayData replayData = ReplayRecorder.LoadFromFile(filePath);
        Debug.Log($"[ReplayTester] Loaded replay: {replayData.Commands.Count} commands");

        // Run replay
        var logger = new UnityLogger();
        var runner = new ReplayRunner(logger);
        ReplayResult result = runner.RunReplay(replayData);

        // Log result
        LogTestResult(result, filePath);

        lastTestResult = result.ToString();
        return result;
      }
      catch (System.Exception ex)
      {
        lastTestResult = $"Error testing replay: {ex.Message}";
        Debug.LogError($"[ReplayTester] {lastTestResult}");
        Debug.LogException(ex);
        return null;
      }
    }

    private void LogTestResult(ReplayResult result, string filePath)
    {
      string filename = Path.GetFileName(filePath);
      string status = result.IsPerfect ? "✓ PERFECT" :
                      result.IsMatchCorrect ? "✓ CORRECT (with drift)" :
                      "✗ FAILED";

      Debug.Log($"[ReplayTester] {status} - {filename}");
      Debug.Log($"  Winner: {result.Winner} (Expected: {result.ExpectedWinner})");
      Debug.Log($"  Ticks: {result.ReplayedTicks} (Expected: {result.ExpectedTicks})");
      
      if (result.HasDrift)
      {
        Debug.LogWarning($"  Drift detected at tick {result.FirstDriftTick}");
      }

      if (!result.IsMatchCorrect)
      {
        Debug.LogError($"  REPLAY MISMATCH! Expected {result.ExpectedWinner} to win, but got {result.Winner}");
      }
    }

    /// <summary>
    /// Compare two replay files to see if they produce the same outcome.
    /// Useful for testing determinism.
    /// </summary>
    [ContextMenu("Compare Two Replays")]
    public void CompareTwoReplays()
    {
      var files = Directory.GetFiles(replayFolderPath, "*.replay")
          .OrderByDescending(f => File.GetLastWriteTime(f))
          .Take(2)
          .ToArray();

      if (files.Length < 2)
      {
        Debug.LogWarning("[ReplayTester] Need at least 2 replay files to compare");
        return;
      }

      Debug.Log($"[ReplayTester] Comparing replays:");
      Debug.Log($"  File 1: {Path.GetFileName(files[0])}");
      Debug.Log($"  File 2: {Path.GetFileName(files[1])}");

      var result1 = TestReplay(files[0]);
      var result2 = TestReplay(files[1]);

      if (result1 != null && result2 != null)
      {
        bool winnersMatch = result1.Winner == result2.Winner;
        bool ticksMatch = Mathf.Abs(result1.ReplayedTicks - result2.ReplayedTicks) < 10;

        Debug.Log($"[ReplayTester] Comparison:");
        Debug.Log($"  Winners match: {winnersMatch}");
        Debug.Log($"  Ticks match: {ticksMatch} ({result1.ReplayedTicks} vs {result2.ReplayedTicks})");
      }
    }
  }
}
