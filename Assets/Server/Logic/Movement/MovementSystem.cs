using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ClashShared;

namespace ClashServer
{
  /// <summary>
  /// Steers entities toward the goal decided by TargetingSystem (attack target, or the
  /// nearest enemy building as a march objective) and resolves collisions afterwards.
  /// Target selection itself happens earlier in the tick, in TargetingSystem.UpdateTargets.
  /// </summary>
  public sealed class MovementSystem
  {
    private const int MovementResolveIterations = 6;
    private const float WaypointReachedEpsilon = 0.05f;
    private const float OverlapEpsilon = 0.001f;

    private readonly BoardManager boardManager;
    private readonly TargetingSystem targetingSystem;

    private readonly IMovementStrategy stationaryMovement = new StationaryMovement();
    private readonly IMovementStrategy groundMovement = new GroundPathMovement();
    private readonly IMovementStrategy airMovement = new AirDirectMovement();

    public MovementSystem(BoardManager boardManager, TargetingSystem targetingSystem)
    {
      this.boardManager = boardManager;
      this.targetingSystem = targetingSystem;
    }

    public void UpdatePositions(IReadOnlyList<ServerEntity> liveEntities, uint currentTick)
    {
      var desiredPositions = new Dictionary<int, Vector2>(liveEntities.Count);
      foreach (ServerEntity entity in liveEntities)
        desiredPositions[entity.Id] = GetDesiredPosition(entity, liveEntities, currentTick);

      ResolveMovement(liveEntities, desiredPositions);
    }

    private Vector2 GetDesiredPosition(ServerEntity entity, IReadOnlyList<ServerEntity> liveEntities, uint currentTick)
    {
      if (entity.Stats.MovePerTick <= 0f || entity.Definition.Movement == MovementKind.Stationary)
        return entity.Position;

      if (entity.Target != null && targetingSystem.IsInRange(entity, entity.Target, entity.Definition.Attack.Range))
      {
        entity.ClearPath();
        return entity.Position;
      }

      Vector2 goal = ResolveGoal(entity, liveEntities);
      var query = new MovementQuery(goal, currentTick, boardManager);
      return GetStrategy(entity).GetDesiredPosition(entity, in query);
    }

    // Chase the attack target when we have one; otherwise march on the nearest enemy
    // building (Clash Royale lane push). Straight forward is a last resort when the
    // enemy has no buildings left.
    private Vector2 ResolveGoal(ServerEntity entity, IReadOnlyList<ServerEntity> liveEntities)
    {
      if (entity.Target != null)
        return entity.Target.Position;

      ServerEntity objective = targetingSystem.FindMarchObjective(entity, liveEntities);
      if (objective != null)
        return objective.Position;

      return new Vector2(entity.Position.X, entity.Team == EntityTeam.Team1 ? NavGrid.WORLD_TOP : NavGrid.WORLD_BOTTOM);
    }

    private IMovementStrategy GetStrategy(ServerEntity entity) => entity.Definition.Movement switch
    {
      MovementKind.Air => airMovement,
      MovementKind.Stationary => stationaryMovement,
      _ => groundMovement
    };

    private void ResolveMovement(IReadOnlyList<ServerEntity> liveEntities, Dictionary<int, Vector2> desiredPositions)
    {
      List<ServerEntity> groundMobile = liveEntities
        .Where(entity => !entity.IsBuilding && entity.CollisionRadius > 0f && entity.Definition.Movement != MovementKind.Air)
        .ToList();
      List<ServerEntity> airMobile = liveEntities
        .Where(entity => !entity.IsBuilding && entity.CollisionRadius > 0f && entity.Definition.Movement == MovementKind.Air)
        .ToList();

      if (groundMobile.Count == 0 && airMobile.Count == 0)
        return;

      List<ServerEntity> buildings = liveEntities
        .Where(entity => entity.IsBuilding && entity.CollisionRadius > 0f)
        .ToList();

      var resolvedPositions = new Dictionary<int, Vector2>(groundMobile.Count + airMobile.Count);
      foreach (ServerEntity entity in groundMobile)
        resolvedPositions[entity.Id] = MovementMath.ClampToArena(desiredPositions[entity.Id], entity.CollisionRadius);
      foreach (ServerEntity entity in airMobile)
        resolvedPositions[entity.Id] = MovementMath.ClampToArena(desiredPositions[entity.Id], entity.CollisionRadius);

      // Ground: resolve building collisions + ground-ground overlap
      for (int iteration = 0; iteration < MovementResolveIterations; iteration++)
      {
        bool movedAny = false;
        foreach (ServerEntity entity in groundMobile)
          movedAny |= ResolveStaticCollisions(entity, resolvedPositions, buildings);
        for (int i = 0; i < groundMobile.Count; i++)
          for (int j = i + 1; j < groundMobile.Count; j++)
            movedAny |= ResolveUnitOverlap(groundMobile[i], groundMobile[j], resolvedPositions, desiredPositions);
        if (!movedAny) break;
      }

      // Air: only resolve air-air overlap (fly over buildings and ground units)
      for (int iteration = 0; iteration < MovementResolveIterations; iteration++)
      {
        bool movedAny = false;
        for (int i = 0; i < airMobile.Count; i++)
          for (int j = i + 1; j < airMobile.Count; j++)
            movedAny |= ResolveUnitOverlap(airMobile[i], airMobile[j], resolvedPositions, desiredPositions);
        if (!movedAny) break;
      }

      foreach (ServerEntity entity in groundMobile)
      {
        entity.MoveTo(KeepOnWalkableCell(entity, MovementMath.ClampToArena(resolvedPositions[entity.Id], entity.CollisionRadius)));
        AdvancePathProgress(entity);
      }
      foreach (ServerEntity entity in airMobile)
        entity.MoveTo(MovementMath.ClampToArena(resolvedPositions[entity.Id], entity.CollisionRadius));
    }

    private bool ResolveStaticCollisions(
      ServerEntity entity,
      Dictionary<int, Vector2> resolvedPositions,
      IReadOnlyList<ServerEntity> buildings)
    {
      Vector2 original = resolvedPositions[entity.Id];
      Vector2 adjusted = original;

      foreach (ServerEntity building in buildings)
      {
        float minDistance = entity.CollisionRadius + building.CollisionRadius;
        Vector2 delta = adjusted - building.Position;
        float distanceSq = delta.LengthSquared();

        if (distanceSq >= minDistance * minDistance)
          continue;

        float distance = MathF.Sqrt(distanceSq);
        Vector2 normal = distance > OverlapEpsilon
          ? delta / distance
          : GetDeterministicSeparationDirection(entity.Id, building.Id);

        adjusted = building.Position + normal * (minDistance + OverlapEpsilon);
      }

      adjusted = KeepOnWalkableCell(entity, MovementMath.ClampToArena(adjusted, entity.CollisionRadius));
      if ((adjusted - original).LengthSquared() <= OverlapEpsilon * OverlapEpsilon)
        return false;

      resolvedPositions[entity.Id] = adjusted;
      return true;
    }

    private bool ResolveUnitOverlap(
      ServerEntity a,
      ServerEntity b,
      Dictionary<int, Vector2> resolvedPositions,
      Dictionary<int, Vector2> desiredPositions)
    {
      Vector2 aPos = resolvedPositions[a.Id];
      Vector2 bPos = resolvedPositions[b.Id];
      float minDistance = a.CollisionRadius + b.CollisionRadius;
      Vector2 delta = bPos - aPos;
      float distanceSq = delta.LengthSquared();

      if (distanceSq >= minDistance * minDistance)
        return false;

      float distance = MathF.Sqrt(distanceSq);
      Vector2 normal = distance > OverlapEpsilon
        ? delta / distance
        : GetDeterministicSeparationDirection(a.Id, b.Id);

      float overlap = minDistance - distance + OverlapEpsilon;
      float aDisplacementShare = GetDisplacementShare(a, desiredPositions[a.Id], b, desiredPositions[b.Id]);
      float bDisplacementShare = 1f - aDisplacementShare;

      aPos -= normal * overlap * aDisplacementShare;
      bPos += normal * overlap * bDisplacementShare;

      resolvedPositions[a.Id] = MovementMath.ClampToArena(aPos, a.CollisionRadius);
      resolvedPositions[b.Id] = MovementMath.ClampToArena(bPos, b.CollisionRadius);
      return true;
    }

    private float GetDisplacementShare(
      ServerEntity entity,
      Vector2 desiredPosition,
      ServerEntity other,
      Vector2 otherDesiredPosition)
    {
      float entityResistance = GetPushResistance(entity, desiredPosition);
      float otherResistance = GetPushResistance(other, otherDesiredPosition);
      float totalResistance = entityResistance + otherResistance;

      if (totalResistance <= 0f)
        return 0.5f;

      return otherResistance / totalResistance;
    }

    private bool HasMovementIntent(ServerEntity entity, Vector2 desiredPosition)
    {
      return (desiredPosition - entity.Position).LengthSquared() > OverlapEpsilon * OverlapEpsilon;
    }

    private float GetPushResistance(ServerEntity entity, Vector2 desiredPosition)
    {
      float resistance = MathF.Max(entity.PushWeight, 0.1f);

      if (!HasMovementIntent(entity, desiredPosition))
        resistance *= 1.25f;

      return resistance;
    }

    private void AdvancePathProgress(ServerEntity entity)
    {
      while (entity.Path != null &&
             entity.Path.Count > 0 &&
             (entity.Position - entity.Path[0]).LengthSquared() <= WaypointReachedEpsilon * WaypointReachedEpsilon)
      {
        entity.Path.RemoveAt(0);
      }
    }

    private Vector2 KeepOnWalkableCell(ServerEntity entity, Vector2 position)
    {
      var (cx, cy) = NavGrid.WorldToCell(position);
      if (NavGrid.IsWalkable(cx, cy, boardManager))
        return position;

      Vector2 fallback = MovementMath.ClampToArena(entity.Position, entity.CollisionRadius);
      var (fx, fy) = NavGrid.WorldToCell(fallback);
      return NavGrid.IsWalkable(fx, fy, boardManager) ? fallback : position;
    }

    private static Vector2 GetDeterministicSeparationDirection(int firstId, int secondId)
    {
      uint hash = (uint)(firstId * 73856093 ^ secondId * 19349663);
      float angle = (hash & 1023) / 1024f * MathF.PI * 2f;
      return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
    }
  }
}
