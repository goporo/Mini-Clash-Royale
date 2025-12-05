using UnityEngine;

public class CombatBehaviour : IEntityBehaviour
{
  private float cooldown;

  public void Tick(Entity e, float dt)
  {
    cooldown -= dt;

    // Validate + refresh target
    ValidateOrAcquireTarget(e);

    if (e.Target == null || cooldown > 0)
      return;

    // In range? Attack
    if (IsInRange(e, e.Target))
    {
      e.Target.TakeDamage(e.Stats.AttackDamage);
      cooldown = e.Stats.AttackCooldown;
    }
  }

  private void ValidateOrAcquireTarget(Entity e)
  {
    if (e.Target != null && e.Target.IsAlive)
    {
      // Check if target is still within aggro range
      float sqrDist = (e.Target.Position - e.Position).sqrMagnitude;
      float sqrAggro = e.Stats.AggroRange * e.Stats.AggroRange;
      if (sqrDist <= sqrAggro)
        return;
    }

    // Acquire new target (respects aggro range)
    e.Target = TargetingSystem.AcquireTarget(e, e.Director.Entities.Entities);
  }

  private bool IsInRange(Entity e, Entity target)
  {
    float sqrDist = (target.Position - e.Position).sqrMagnitude;
    float sqrRange = e.Stats.AttackRange * e.Stats.AttackRange;
    return sqrDist <= sqrRange;
  }
}
