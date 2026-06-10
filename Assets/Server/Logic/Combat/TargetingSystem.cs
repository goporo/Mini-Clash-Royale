using System.Collections.Generic;
using System.Numerics;

namespace ClashServer
{
  public sealed class TargetingSystem
  {
    public ServerEntity AcquireNearestTarget(ServerEntity entity, IReadOnlyList<ServerEntity> candidates, TargetingRule targeting)
    {
      ServerEntity closest = null;
      float closestDistSq = float.MaxValue;
      float aggroRange = entity.Definition.Attack.Range;

      foreach (ServerEntity other in candidates)
      {
        if (!other.IsAlive || other.Team == entity.Team)
          continue;

        if (targeting.Category == TargetCategory.BuildingsOnly && !other.IsBuilding)
          continue;

        if (!IsOnLayer(other, targeting.Layers))
          continue;

        if ((entity.Definition.Attack.AffectedLayers & other.Layer) == 0)
          continue;

        float effectiveAggroRange = aggroRange + other.FootprintRadius;
        float distSq = (other.Position - entity.Position).LengthSquared();

        if (distSq < effectiveAggroRange * effectiveAggroRange && distSq < closestDistSq)
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

    private static bool IsOnLayer(ServerEntity entity, TargetLayer layer)
    {
      TargetLayer entityLayer = entity.Definition.Movement == MovementKind.Air
        ? TargetLayer.Air
        : TargetLayer.Ground;
      return (layer & entityLayer) != 0;
    }
  }
}
