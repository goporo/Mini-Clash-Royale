using System;
using System.Collections.Generic;
using System.Numerics;
using ClashShared;

namespace ClashServer
{
  // Pure server class representing an in-game entity
  public enum EntityTeam
  {
    Team1 = 0,
    Team2 = 1
  }

  public enum MoveSpeed
  {
    Slow = 1,
    Medium = 2,
    Fast = 3,
    VeryFast = 4
  }

  public struct EntityStats
  {
    public float MaxHP;
    public float CurrentHP;
    public float MoveSpeed;
    public float MovePerTick;
    public float AttackRange;
    public float AttackDamage;
    public float AttackCooldown;
    public int AttackCooldownTicks;
    public float AggroRange;

    public EntityStats(float maxHP, float moveSpeed, float attackRange,
        float attackDamage, float attackCooldown, float aggroRange)
    {
      MaxHP = maxHP;
      CurrentHP = maxHP;
      MoveSpeed = moveSpeed;
      MovePerTick = moveSpeed * ServerMatchController.FIXED_DT;
      AttackRange = attackRange;
      AttackDamage = attackDamage;
      AttackCooldown = attackCooldown;
      AttackCooldownTicks = (int)MathF.Round(attackCooldown / ServerMatchController.FIXED_DT);
      AggroRange = aggroRange;
    }
  }

  public class ServerEntity
  {
    public int Id { get; set; }
    public Vector2 Position { get; set; }
    public EntityTeam Team { get; set; }
    public CardId Type { get; set; }
    public EntityStats Stats { get; set; }
    public ServerEntity Target { get; set; }
    public bool IsAlive { get; set; }
    public int AttackCooldownTicks { get; set; }
    public bool IsBuilding { get; set; }

    // Pathfinding state
    public List<Vector2> Path { get; set; }
    public uint PathRecalcTick { get; set; }

    public ServerEntity(int id, CardId type, Vector2 position, EntityTeam team, bool isBuilding = false)
    {
      Id = id;
      Type = type;
      Position = position;
      Team = team;
      IsBuilding = isBuilding;
      IsAlive = true;
      AttackCooldownTicks = 0;
      Stats = GetStatsForType(type);
    }

    private static EntityStats GetStatsForType(CardId type)
    {
      switch (type)
      {
        case CardId.Knight:
          return new EntityStats(100, 2f, 1.5f, 20f, 1.0f, 5f);
        case CardId.Archer:
          return new EntityStats(50, 1.5f, 5f, 15f, 1f, 6f);
        case CardId.Giant:
          return new EntityStats(300, 1f, 1.5f, 40f, 1f, 5f);
        case CardId.PrincessTower:
          return new EntityStats(500, 0f, 6f, 10f, 1f, 6f);
        case CardId.KingTower:
          return new EntityStats(1000, 0f, 7f, 10f, 1.2f, 7f);
        default:
          return new EntityStats(100, 2f, 1.5f, 20f, 1.5f, 5f);
      }
    }

    public void TakeDamage(float damage)
    {
      var stats = Stats;
      stats.CurrentHP -= damage;
      if (stats.CurrentHP <= 0)
      {
        stats.CurrentHP = 0;
        IsAlive = false;
      }
      Stats = stats;
    }
  }
}
