using System.Collections.Generic;
using UnityEngine;

public class GameplayDirector
{
  public readonly EntitySystem Entities = new EntitySystem();

  public void Register(Entity e)
  {
    Entities.Add(e);
  }

  public void Remove(Entity e)
  {
    Entities.MarkForRemoval(e);
  }

  public void Tick(float dt)
  {
    // Update behaviours
    foreach (var e in Entities.Entities)
      e.Tick(dt);

    // Remove dead entities
    Entities.Cleanup();
  }

  public static void ApplySeparation(IReadOnlyList<Entity> all)
  {
    const float pushStrength = 5f;

    for (int i = 0; i < all.Count; i++)
    {
      for (int j = i + 1; j < all.Count; j++)
      {
        var a = all[i];
        var b = all[j];

        if (a.Team != b.Team) continue; // Only separate same team (CR-style)

        Vector3 delta = b.Position - a.Position;
        float dist = delta.magnitude;
        float minDist = a.Stats.Radius + b.Stats.Radius;

        if (dist < minDist && dist > 0.001f)
        {
          Vector3 push = delta.normalized * (minDist - dist) * 0.5f;

          a.Position -= push * pushStrength * Time.deltaTime;
          b.Position += push * pushStrength * Time.deltaTime;
        }
      }
    }
  }
}
