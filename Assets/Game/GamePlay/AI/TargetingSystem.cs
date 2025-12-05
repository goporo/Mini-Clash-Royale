using UnityEngine;
using System.Collections.Generic;

public static class TargetingSystem
{
  // returns closest enemy within aggro radius
  public static Entity AcquireTarget(Entity self, List<Entity> all)
  {
    float bestDist = float.MaxValue;
    Entity best = null;

    float sqrAggroRange = self.Stats.AggroRange * self.Stats.AggroRange;

    foreach (var e in all)
    {
      if (e.Team == self.Team || !e.IsAlive) continue;

      float d = (e.Position - self.Position).sqrMagnitude;
      if (d <= sqrAggroRange && d < bestDist)
      {
        bestDist = d;
        best = e;
      }
    }

    return best;
  }

  public static Entity FindNearestEnemy(Entity self, IReadOnlyList<Entity> all)
  {
    Entity closest = null;
    float bestDist = float.MaxValue;

    foreach (var e in all)
    {
      if (e.Team == self.Team) continue;
      if (!e.IsAlive) continue;

      float dist = (e.Position - self.Position).sqrMagnitude;
      if (dist < bestDist)
      {
        bestDist = dist;
        closest = e;
      }
    }

    return closest;
  }
}

