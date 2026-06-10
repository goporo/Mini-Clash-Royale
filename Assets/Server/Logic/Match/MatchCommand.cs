using System;
using System.Numerics;
using ClashShared;

namespace ClashServer
{

  [Serializable]
  public struct MatchCommand
  {
    public int Tick;
    public int PlayerId;
    public CommandType Type;
    public CardId CardId;
    public Vector2 Position;
    public int TargetTile;

    public MatchCommand(int tick, int playerId, CommandType type)
    {
      Tick = tick;
      PlayerId = playerId;
      Type = type;
      CardId = CardId.None;
      Position = Vector2.Zero;
      TargetTile = -1;
    }

    public static MatchCommand PlayCard(int tick, int playerId, CardId cardId, Vector2 position)
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
        CardId = CardId.None,
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
