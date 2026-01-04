using System;
using System.Numerics;

namespace ClashServer
{
  /// <summary>
  /// Represents a single player command in the match.
  /// This is the ONLY thing we log for replay.
  /// Keep it minimal - no state, only commands.
  /// </summary>
  [Serializable]
  public struct MatchCommand
  {
    public int Tick;
    public int PlayerId;
    public CommandType Type;
    public int CardId;          // For PlayCard
    public Vector2 Position;    // For PlayCard spawn position
    public int TargetTile;      // For future use (if needed)

    public MatchCommand(int tick, int playerId, CommandType type)
    {
      Tick = tick;
      PlayerId = playerId;
      Type = type;
      CardId = -1;
      Position = Vector2.Zero;
      TargetTile = -1;
    }

    public static MatchCommand PlayCard(int tick, int playerId, int cardId, Vector2 position)
    {
      return new MatchCommand
      {
        Tick = tick,
        PlayerId = playerId,
        Type = CommandType.PlayCard,
        CardId = cardId,
        Position = position,
        TargetTile = -1
      };
    }

    public static MatchCommand Surrender(int tick, int playerId)
    {
      return new MatchCommand
      {
        Tick = tick,
        PlayerId = playerId,
        Type = CommandType.Surrender,
        CardId = -1,
        Position = Vector2.Zero,
        TargetTile = -1
      };
    }

    public override string ToString()
    {
      return $"[T{Tick}] P{PlayerId} {Type}" +
             (Type == CommandType.PlayCard ? $" Card={CardId} Pos={Position}" : "");
    }
  }

  public enum CommandType
  {
    PlayCard,
    Emote,
    Surrender
  }
}
