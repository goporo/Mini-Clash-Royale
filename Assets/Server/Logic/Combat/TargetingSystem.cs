using System;
using System.Collections.Generic;
using System.Numerics;

namespace ClashServer
{
  /// <summary>
  /// Single authority for target selection. GameplayDirector runs <see cref="UpdateTargets"/>
  /// once per tick before movement, so MovementSystem and CombatSystem always act on the
  /// same decision instead of each acquiring targets independently.
  ///
  /// Clash Royale rules implemented here:
  ///  - Troops aggro enemies inside sight range (TargetingRule.SightRange, never below attack range)
  ///    and stay locked on until the target dies or becomes invalid (kiting works).
  ///  - Buildings drop a target that leaves attack range and re-acquire.
  ///  - Building-only units (Giant, ...) always know where the nearest enemy building is.
  /// </summary>
  public sealed class TargetingSystem
  {
    public void UpdateTargets(IReadOnlyList<ServerEntity> liveEntities)
    {
      foreach (ServerEntity entity in liveEntities)
      {
        if (entity.Definition.Attack.Kind == AttackKind.None)
          continue;

        ServerEntity target = ValidateCurrentTarget(entity) ?? AcquireTarget(entity, liveEntities);
        entity.SetTarget(target);
      }
    }

    private ServerEntity ValidateCurrentTarget(ServerEntity entity)
    {
      ServerEntity current = entity.Target;
      if (current == null || !IsValidTarget(entity, current))
        return null;

      // Buildings (towers, cannon) drop targets that leave attack range; troops stay locked.
      if (entity.IsBuilding && !IsInRange(entity, current, entity.Definition.Attack.Range))
        return null;

      return current;
    }

    private ServerEntity AcquireTarget(ServerEntity entity, IReadOnlyList<ServerEntity> candidates)
    {
      TargetingRule targeting = entity.Definition.Targeting;
      bool unlimitedSight = targeting.Category == TargetCategory.BuildingsOnly;
      float sightRange = MathF.Max(targeting.SightRange, entity.Definition.Attack.Range);

      ServerEntity closest = null;
      float closestDistSq = float.MaxValue;

      foreach (ServerEntity other in candidates)
      {
        if (!IsValidTarget(entity, other))
          continue;

        float distSq = (other.Position - entity.Position).LengthSquared();

        if (!unlimitedSight)
        {
          float effectiveSight = sightRange + other.FootprintRadius;
          if (distSq > effectiveSight * effectiveSight)
            continue;
        }

        if (distSq < closestDistSq)
        {
          closest = other;
          closestDistSq = distSq;
        }
      }

      return closest;
    }

    private static bool IsValidTarget(ServerEntity entity, ServerEntity other)
    {
      if (!other.IsAlive || other.Team == entity.Team)
        return false;

      TargetingRule targeting = entity.Definition.Targeting;
      if (targeting.Category == TargetCategory.BuildingsOnly && !other.IsBuilding)
        return false;

      if ((targeting.Layers & other.Layer) == 0)
        return false;

      if ((entity.Definition.Attack.AffectedLayers & other.Layer) == 0)
        return false;

      return true;
    }

    /// <summary>
    /// Where a unit with no attack target marches: the nearest enemy building,
    /// like Clash Royale units pushing a lane. Null when no enemy building remains.
    /// </summary>
    public ServerEntity FindMarchObjective(ServerEntity entity, IReadOnlyList<ServerEntity> candidates)
    {
      ServerEntity closest = null;
      float closestDistSq = float.MaxValue;

      foreach (ServerEntity other in candidates)
      {
        if (!other.IsAlive || other.Team == entity.Team || !other.IsBuilding)
          continue;

        float distSq = (other.Position - entity.Position).LengthSquared();
        if (distSq < closestDistSq)
        {
          closest = other;
          closestDistSq = distSq;
        }
      }

      return closest;
    }

    public bool IsInRange(ServerEntity entity, ServerEntity target, float range)
    {
      float effectiveRange = range + target.FootprintRadius;
      float distSq = (target.Position - entity.Position).LengthSquared();
      return distSq <= effectiveRange * effectiveRange;
    }

    public bool IsWithinRadius(Vector2 center, ServerEntity target, float radius)
    {
      float effectiveRadius = radius + target.FootprintRadius;
      float distSq = (target.Position - center).LengthSquared();
      return distSq <= effectiveRadius * effectiveRadius;
    }
  }
}
