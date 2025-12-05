public class TowerAttackBehaviour : IEntityBehaviour
{
  private float cd;

  public void Tick(Entity e, float dt)
  {
    cd -= dt;
    if (cd > 0) return;

    var target = TargetingSystem.FindNearestEnemy(e, e.Director.Entities.Entities);
    if (target == null) return;

    float dist = (target.Position - e.Position).sqrMagnitude;
    float attackRange = e.Stats.AttackRange * e.Stats.AttackRange;

    if (dist <= attackRange)
    {
      target.TakeDamage(e.Stats.AttackDamage);
      cd = e.Stats.AttackCooldown;
    }
  }
}
