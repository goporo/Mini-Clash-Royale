using System;
using System.Collections.Generic;
using System.Linq;

namespace ClashServer
{
  /// <summary>
  /// Stores and organizes match commands by tick for deterministic replay.
  /// Commands are indexed by tick for O(1) access during replay.
  /// </summary>
  public class CommandLog
  {
    private List<MatchCommand> commands = new List<MatchCommand>();
    private Dictionary<int, List<MatchCommand>> commandsByTick = new Dictionary<int, List<MatchCommand>>();

    /// <summary>
    /// Add a command to the log. Commands should only be added AFTER validation.
    /// </summary>
    public void AddCommand(MatchCommand command)
    {
      commands.Add(command);

      if (!commandsByTick.ContainsKey(command.Tick))
      {
        commandsByTick[command.Tick] = new List<MatchCommand>();
      }
      commandsByTick[command.Tick].Add(command);
    }

    /// <summary>
    /// Get all commands for a specific tick. Returns empty list if no commands.
    /// </summary>
    public List<MatchCommand> GetCommandsForTick(int tick)
    {
      if (commandsByTick.TryGetValue(tick, out var tickCommands))
      {
        return tickCommands;
      }
      return new List<MatchCommand>();
    }

    /// <summary>
    /// Get all commands in chronological order.
    /// </summary>
    public List<MatchCommand> GetAllCommands()
    {
      return new List<MatchCommand>(commands);
    }

    /// <summary>
    /// Clear all logged commands.
    /// </summary>
    public void Clear()
    {
      commands.Clear();
      commandsByTick.Clear();
    }

    /// <summary>
    /// Get the total number of commands logged.
    /// </summary>
    public int Count => commands.Count;

    /// <summary>
    /// Get the range of ticks that have commands.
    /// </summary>
    public (int minTick, int maxTick) GetTickRange()
    {
      if (commands.Count == 0)
        return (0, 0);

      return (commands.Min(c => c.Tick), commands.Max(c => c.Tick));
    }

    /// <summary>
    /// Create a deep copy of this log.
    /// </summary>
    public CommandLog Clone()
    {
      var clone = new CommandLog();
      foreach (var cmd in commands)
      {
        clone.AddCommand(cmd);
      }
      return clone;
    }

    public override string ToString()
    {
      if (commands.Count == 0)
        return "CommandLog: Empty";

      var (minTick, maxTick) = GetTickRange();
      return $"CommandLog: {commands.Count} commands (Tick {minTick} to {maxTick})";
    }
  }
}
