using System;
using System.Numerics;

namespace ClashServer
{
  /// <summary>Per-tick inputs a strategy needs to steer an entity toward its goal.</summary>
  public readonly struct MovementQuery
  {
    public MovementQuery(Vector2 goal, uint currentTick, BoardManager board)
    {
      Goal = goal;
      CurrentTick = currentTick;
      Board = board;
    }

    public Vector2 Goal { get; }
    public uint CurrentTick { get; }
    public BoardManager Board { get; }
  }

  /// <summary>
  /// How an entity turns "where I want to go" into a desired position for this tick.
  /// Collision resolution afterwards is shared and lives in MovementSystem.
  /// </summary>
  public interface IMovementStrategy
  {
    Vector2 GetDesiredPosition(ServerEntity entity, in MovementQuery query);
  }

  public sealed class StationaryMovement : IMovementStrategy
  {
    public Vector2 GetDesiredPosition(ServerEntity entity, in MovementQuery query) => entity.Position;
  }

  /// <summary>Air units ignore terrain and fly straight at the goal.</summary>
  public sealed class AirDirectMovement : IMovementStrategy
  {
    public Vector2 GetDesiredPosition(ServerEntity entity, in MovementQuery query)
      => MovementMath.MoveTowards(entity.Position, query.Goal, entity.EffectiveMovePerTick);
  }

  /// <summary>Ground units follow A* waypoints around the river/buildings, recalculated periodically.</summary>
  public sealed class GroundPathMovement : IMovementStrategy
  {
    private const int PathRecalcInterval = 3;

    public Vector2 GetDesiredPosition(ServerEntity entity, in MovementQuery query)
    {
      if (entity.Path == null || query.CurrentTick >= entity.PathRecalcTick)
      {
        entity.SetPath(
          Pathfinder.FindPath(entity.Position, query.Goal, query.Board),
          query.CurrentTick + PathRecalcInterval);
      }

      if (entity.Path != null && entity.Path.Count > 0)
        return MovementMath.MoveTowards(entity.Position, entity.Path[0], entity.EffectiveMovePerTick);

      // No waypoints left (same cell as goal, or no path found): step straight at the goal.
      // Collision resolution keeps us out of obstacles if the direct line is blocked.
      return MovementMath.MoveTowards(entity.Position, query.Goal, entity.EffectiveMovePerTick);
    }
  }

  public static class MovementMath
  {
    private const float Epsilon = 0.001f;

    public static Vector2 MoveTowards(Vector2 from, Vector2 to, float maxDistance)
    {
      Vector2 delta = to - from;
      float distance = delta.Length();

      if (distance <= maxDistance || distance <= Epsilon)
        return to;

      return from + delta / distance * maxDistance;
    }

    public static Vector2 ClampToArena(Vector2 position, float radius)
    {
      return new Vector2(
        Math.Clamp(position.X, NavGrid.WORLD_LEFT + radius, NavGrid.WORLD_RIGHT - radius),
        Math.Clamp(position.Y, NavGrid.WORLD_BOTTOM + radius, NavGrid.WORLD_TOP - radius));
    }
  }
}
