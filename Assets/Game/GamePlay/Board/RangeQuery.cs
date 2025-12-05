using System.Collections.Generic;
using UnityEngine;

// for TargetingSystem
public static class RangeQuery
{
  public static List<Entity> EntitiesInRadius(
      Vector3 center,
      float radius,
      IReadOnlyList<Entity> entities)
  {
    var results = new List<Entity>();
    float r2 = radius * radius;

    foreach (var e in entities)
    {
      if ((e.Position - center).sqrMagnitude <= r2)
        results.Add(e);
    }

    return results;
  }

  public static Entity NearestEntity(
      Vector3 pos,
      IReadOnlyList<Entity> entities)
  {
    Entity closest = null;
    float bestDist = float.MaxValue;

    foreach (var e in entities)
    {
      float dist = (e.Position - pos).sqrMagnitude;
      if (dist < bestDist)
      {
        bestDist = dist;
        closest = e;
      }
    }

    return closest;
  }
}
