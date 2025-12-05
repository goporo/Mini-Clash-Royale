using UnityEngine;

[CreateAssetMenu(menuName = "Game/Domain/Building")]
public class BuildingConfig : EntityConfigBase
{
  public float maxHP;
  public float attackDamage;
  public float attackRange;
  public float attackCooldown;

  public bool wakeKingOnlyWhenPrincessDies = true;

  public EntityStats CreateStats() =>
      new EntityStats(maxHP)
      {
        AttackDamage = attackDamage,
        AttackRange = attackRange,
        AttackCooldown = attackCooldown
      };
}
