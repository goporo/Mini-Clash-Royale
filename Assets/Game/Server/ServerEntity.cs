using UnityEngine;

namespace ClashServer
{
  // Pure server class representing an in-game entity
  public enum EntityTeam
  {
    Team1 = 0,
    Team2 = 1
  }

  public struct EntityStats
  {
    public float MaxHP;
    public float CurrentHP;
    public float MoveSpeed;
    public float AttackRange;
    public float AttackDamage;
    public float AttackSpeed;
    public float AggroRange;

    public EntityStats(float maxHP, float moveSpeed, float attackRange,
        float attackDamage, float attackSpeed, float aggroRange)
    {
      MaxHP = maxHP;
      CurrentHP = maxHP;
      MoveSpeed = moveSpeed;
      AttackRange = attackRange;
      AttackDamage = attackDamage;
      AttackSpeed = attackSpeed;
      AggroRange = aggroRange;
    }
  }

  // Server-side entity - no Unity dependencies for easy .NET migration
  public class ServerEntity
  {
    public int Id { get; set; }
    public Vector2 Position { get; set; }
    public EntityTeam Team { get; set; }
    public string Type { get; set; }
    public EntityStats Stats { get; set; }
    public ServerEntity Target { get; set; }
    public bool IsAlive { get; set; }
    public float AttackCooldown { get; set; }
    public bool IsBuilding { get; set; }

    public ServerEntity(int id, string type, Vector2 position, EntityTeam team, bool isBuilding = false)
    {
      Id = id;
      Type = type;
      Position = position;
      Team = team;
      IsBuilding = isBuilding;
      IsAlive = true;
      AttackCooldown = 0;
      Stats = GetStatsForType(type);
    }

    private static EntityStats GetStatsForType(string type)
    {
      // TODO: Load from ScriptableObject or config file
      switch (type.ToLower())
      {
        case "knight":
          return new EntityStats(100, 2f, 1.5f, 20f, 1.5f, 5f);
        case "archer":
          return new EntityStats(50, 1.5f, 5f, 15f, 1f, 6f);
        case "giant":
          return new EntityStats(300, 1f, 1.5f, 40f, 1f, 5f);
        case "tower":
          return new EntityStats(5000, 0f, 6f, 50f, 1f, 6f);
        case "kingtower":
          return new EntityStats(10000, 0f, 7f, 60f, 1.2f, 7f);
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
