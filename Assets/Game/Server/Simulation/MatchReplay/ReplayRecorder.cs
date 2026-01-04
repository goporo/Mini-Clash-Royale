using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ClashServer
{
  /// <summary>
  /// Records match commands and saves them to disk for replay.
  /// Provides serialization/deserialization for replay files.
  /// </summary>
  public class ReplayRecorder
  {
    private CommandLog commandLog = new CommandLog();
    private List<(int tick, int hash)> stateHashes = new List<(int, int)>();
    private MatchMetadata metadata;
    private bool isRecording = false;

    public bool IsRecording => isRecording;
    public CommandLog CommandLog => commandLog;

    public void StartRecording(MatchMetadata matchMetadata)
    {
      if (isRecording)
      {
        throw new InvalidOperationException("Already recording a match");
      }

      metadata = matchMetadata;
      commandLog.Clear();
      stateHashes.Clear();
      isRecording = true;
    }

    public void RecordCommand(MatchCommand command)
    {
      if (!isRecording)
      {
        throw new InvalidOperationException("Not currently recording");
      }

      commandLog.AddCommand(command);
    }

    public void RecordStateHash(int tick, int hash)
    {
      if (!isRecording)
        return;

      stateHashes.Add((tick, hash));
    }

    public void StopRecording(EntityTeam? winner, int finalTick)
    {
      if (!isRecording)
        return;

      metadata.Winner = winner;
      metadata.FinalTick = finalTick;
      metadata.EndTime = DateTime.UtcNow;
      isRecording = false;
    }

    /// <summary>
    /// Save the recorded replay to a file.
    /// </summary>
    public void SaveToFile(string filePath)
    {
      if (isRecording)
      {
        throw new InvalidOperationException("Cannot save while still recording");
      }

      var replay = new ReplayData
      {
        Metadata = metadata,
        Commands = commandLog.GetAllCommands(),
        StateHashes = stateHashes
      };

      string json = SerializeReplay(replay);
      File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Load a replay from a file.
    /// </summary>
    public static ReplayData LoadFromFile(string filePath)
    {
      if (!File.Exists(filePath))
      {
        throw new FileNotFoundException($"Replay file not found: {filePath}");
      }

      string json = File.ReadAllText(filePath);
      return DeserializeReplay(json);
    }

    private string SerializeReplay(ReplayData replay)
    {
      var sb = new StringBuilder();
      sb.AppendLine("=== MATCH REPLAY ===");
      sb.AppendLine($"Version: {replay.Metadata.Version}");
      sb.AppendLine($"MatchId: {replay.Metadata.MatchId}");
      sb.AppendLine($"StartTime: {replay.Metadata.StartTime:yyyy-MM-dd HH:mm:ss}");
      sb.AppendLine($"EndTime: {replay.Metadata.EndTime:yyyy-MM-dd HH:mm:ss}");
      sb.AppendLine($"Winner: {replay.Metadata.Winner}");
      sb.AppendLine($"FinalTick: {replay.Metadata.FinalTick}");
      sb.AppendLine($"Player1: {replay.Metadata.Player1Name}");
      sb.AppendLine($"Player2: {replay.Metadata.Player2Name}");
      sb.AppendLine();

      sb.AppendLine("=== COMMANDS ===");
      foreach (var cmd in replay.Commands)
      {
        sb.AppendLine($"{cmd.Tick}|{cmd.PlayerId}|{cmd.Type}|{cmd.CardId}|{cmd.Position.X}|{cmd.Position.Y}|{cmd.TargetTile}");
      }
      sb.AppendLine();

      sb.AppendLine("=== STATE HASHES ===");
      foreach (var (tick, hash) in replay.StateHashes)
      {
        sb.AppendLine($"{tick}|{hash}");
      }

      return sb.ToString();
    }

    private static ReplayData DeserializeReplay(string content)
    {
      var replay = new ReplayData();
      var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

      string section = "";
      foreach (var line in lines)
      {
        if (line.StartsWith("==="))
        {
          section = line;
          continue;
        }

        if (section.Contains("MATCH REPLAY"))
        {
          ParseMetadataLine(line, replay.Metadata);
        }
        else if (section.Contains("COMMANDS"))
        {
          ParseCommandLine(line, replay.Commands);
        }
        else if (section.Contains("STATE HASHES"))
        {
          ParseHashLine(line, replay.StateHashes);
        }
      }

      return replay;
    }

    private static void ParseMetadataLine(string line, MatchMetadata metadata)
    {
      var parts = line.Split(new[] { ": " }, 2, StringSplitOptions.None);
      if (parts.Length != 2) return;

      string key = parts[0].Trim();
      string value = parts[1].Trim();

      switch (key)
      {
        case "Version":
          metadata.Version = int.Parse(value);
          break;
        case "MatchId":
          metadata.MatchId = value;
          break;
        case "StartTime":
          metadata.StartTime = DateTime.Parse(value);
          break;
        case "EndTime":
          metadata.EndTime = DateTime.Parse(value);
          break;
        case "Winner":
          if (value != "")
            metadata.Winner = (EntityTeam)Enum.Parse(typeof(EntityTeam), value);
          break;
        case "FinalTick":
          metadata.FinalTick = int.Parse(value);
          break;
        case "Player1":
          metadata.Player1Name = value;
          break;
        case "Player2":
          metadata.Player2Name = value;
          break;
      }
    }

    private static void ParseCommandLine(string line, List<MatchCommand> commands)
    {
      var parts = line.Split('|');
      if (parts.Length < 7) return;

      var cmd = new MatchCommand
      {
        Tick = int.Parse(parts[0]),
        PlayerId = int.Parse(parts[1]),
        Type = (CommandType)Enum.Parse(typeof(CommandType), parts[2]),
        CardId = int.Parse(parts[3]),
        Position = new System.Numerics.Vector2(float.Parse(parts[4]), float.Parse(parts[5])),
        TargetTile = int.Parse(parts[6])
      };
      commands.Add(cmd);
    }

    private static void ParseHashLine(string line, List<(int tick, int hash)> hashes)
    {
      var parts = line.Split('|');
      if (parts.Length < 2) return;

      hashes.Add((int.Parse(parts[0]), int.Parse(parts[1])));
    }
  }

  /// <summary>
  /// Metadata about a match for replay purposes.
  /// </summary>
  [Serializable]
  public class MatchMetadata
  {
    public int Version = 1;
    public string MatchId = Guid.NewGuid().ToString();
    public DateTime StartTime = DateTime.UtcNow;
    public DateTime EndTime;
    public EntityTeam? Winner;
    public int FinalTick;
    public string Player1Name = "Player1";
    public string Player2Name = "AI";

    public MatchMetadata() { }

    public MatchMetadata(string player1, string player2)
    {
      Player1Name = player1;
      Player2Name = player2;
    }
  }

  /// <summary>
  /// Complete replay data structure.
  /// </summary>
  [Serializable]
  public class ReplayData
  {
    public MatchMetadata Metadata = new MatchMetadata();
    public List<MatchCommand> Commands = new List<MatchCommand>();
    public List<(int tick, int hash)> StateHashes = new List<(int, int)>();
  }
}
