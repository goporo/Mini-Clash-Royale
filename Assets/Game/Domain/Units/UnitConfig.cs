using UnityEngine;

[CreateAssetMenu(menuName = "Game/Domain/UnitConfig")]
public class UnitConfig : EntityConfigBase
{
  public string unitName;

  [Header("Stats")]
  public float maxHP = 100;
  public float moveSpeed = 2f;
  public float attackDamage = 10f;
  public float attackRange = 1.5f;
  public float attackCooldown = 1f;
  public float radius = 0.5f;

  [Header("Spawn")]
  public Vector3 spawnDirection = Vector3.forward;

  public EntityStats CreateStats()
  {
    return new EntityStats(maxHP)
    {
      MoveSpeed = moveSpeed,
      AttackDamage = attackDamage,
      AttackRange = attackRange,
      AttackCooldown = attackCooldown
    };
  }


}