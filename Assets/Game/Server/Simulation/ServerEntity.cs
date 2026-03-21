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
    public CardId Type { get; set; }
    public Vector2 Position { get; set; }
    public EntityTeam Team { get; set; }
    public EntityStats Stats { get; set; }
    public ServerEntity Target { get; set; }
    public bool IsAlive { get; set; }
    public int AttackCooldownTicks { get; set; }
    public bool IsBuilding { get; set; }
    public float FootprintRadius { get; }
    public float CollisionRadius { get; }
    public float PushWeight { get; }

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
      FootprintRadius = GetFootprintRadius(type);
      CollisionRadius = GetCollisionRadius(type);
      PushWeight = GetPushWeight(type);
    }

    private static float GetFootprintRadius(CardId type) => type switch
    {
      CardId.PrincessTower => 1.5f,  // 3x3
      CardId.KingTower => 2.0f,  // 4x4
      CardId.Cannon => 1.5f,  // 3x3
      _ => 0f,
    };

    private static float GetCollisionRadius(CardId type) => type switch
    {
      CardId.PrincessTower => 1.5f,
      CardId.KingTower => 2.0f,
      CardId.Cannon => 1.5f,
      CardId.Giant => 0.7f,
      CardId.Knight => 0.45f,
      CardId.MiniPekka => 0.4f,
      CardId.Musketeer => 0.4f,
      CardId.Archer => 0.35f,
      CardId.Goblin => 0.3f,
      _ => 0f,
    };

    private static float GetPushWeight(CardId type) => type switch
    {
      CardId.PrincessTower => 100f,
      CardId.KingTower => 120f,
      CardId.Cannon => 100f,
      CardId.Giant => 10f,
      CardId.MiniPekka => 2.2f,
      CardId.Knight => 1.8f,
      CardId.Musketeer => 1.2f,
      CardId.Archer => 0.9f,
      CardId.Goblin => 0.6f,
      _ => 1f,
    };

    private static EntityStats GetStatsForType(CardId type)
    {
      switch (type)
      {

        case CardId.PrincessTower:
          return new EntityStats(500, 0f, 8f, 10f, 1f, 6f);
        case CardId.KingTower:
          return new EntityStats(1000, 0f, 8f, 10f, 1.2f, 7f);
        case CardId.Knight:
          return new EntityStats(100, 2f, 1.5f, 20f, 1.0f, 5f);
        case CardId.Archer:
          return new EntityStats(50, 1.5f, 5f, 15f, 1f, 6f);
        case CardId.Giant:
          return new EntityStats(300, 1f, 1.5f, 40f, 1f, 5f);
        case CardId.Cannon:
          return new EntityStats(150, 0f, 8f, 30f, 1.5f, 5f);
        case CardId.Goblin:
          return new EntityStats(30, 2.5f, 1f, 10f, 0.8f, 4f);
        case CardId.Fireball:
          return new EntityStats(0, 0f, 0f, 100f, 0f, 0f);
        case CardId.Musketeer:
          return new EntityStats(80, 1.5f, 4f, 25f, 1f, 6f);
        case CardId.MiniPekka:
          return new EntityStats(70, 2f, 1.5f, 75f, 0.5f, 5f);
        default:
          throw new ArgumentException($"Unknown card type: {type}");
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
