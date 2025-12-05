public class EntityStats
{
  public float MaxHP { get; private set; }
  public float CurrentHP { get; set; }

  public float MoveSpeed;
  public float AttackDamage;
  public float AttackRange;
  public float AttackCooldown;
  public float Radius;
  public float AggroRange;

  public EntityStats(float maxHp)
  {
    MaxHP = maxHp;
    CurrentHP = maxHp;
    AggroRange = 5f; // default aggro range

  }
}
