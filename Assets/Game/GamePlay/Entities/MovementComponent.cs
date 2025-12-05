using UnityEngine;

public class MovementComponent : IEntityBehaviour
{
  private Vector3 baseDir;

  public MovementComponent(Vector3 direction)
  {
    baseDir = direction.normalized;
  }

  public void Tick(Entity e, float dt)
  {
    // Try acquire a new better target if existing is invalid or out of aggro radius
    if (!HasValidTarget(e))
      e.Target = TargetingSystem.AcquireTarget(e, e.Director.Entities.Entities);

    // Stop if attacking something in range
    if (IsInAttackRange(e))
      return;

    // Determine movement direction
    Vector3 dir;

    if (e.Target != null && e.Target.IsAlive)
    {
      // Move toward target (Clash Royale behavior: redirect to enemy in aggro)
      dir = (e.Target.Position - e.Position).normalized;
    }
    else
    {
      // No target → move forward along lane
      dir = (e.Team == EntityTeam.Team1) ? baseDir : -baseDir;
    }

    e.Position += dir * e.Stats.MoveSpeed * dt;
  }

  bool HasValidTarget(Entity e)
  {
    if (e.Target == null || !e.Target.IsAlive) return false;
    float dist = (e.Target.Position - e.Position).sqrMagnitude;
    return dist <= e.Stats.AggroRange * e.Stats.AggroRange;
  }

  bool IsInAttackRange(Entity e)
  {
    if (e.Target == null) return false;
    float dist = (e.Target.Position - e.Position).sqrMagnitude;
    return dist <= e.Stats.AttackRange * e.Stats.AttackRange;
  }
}
