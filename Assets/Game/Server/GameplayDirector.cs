using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ClashServer
{
  // Pure server simulation - no Mirror dependencies
  // This class can be extracted to standalone .NET easily
  public class GameplayDirector
  {
    private List<ServerEntity> entities = new List<ServerEntity>();
    private int nextEntityId = 1;

    // Main update loop - called every server tick
    public void Update(float deltaTime)
    {
      foreach (var entity in entities.ToList())
      {
        if (!entity.IsAlive) continue;

        UpdateMovement(entity, deltaTime);
        UpdateCombat(entity, deltaTime);
      }

      // Remove dead entities
      entities.RemoveAll(e => !e.IsAlive);
    }

    private void UpdateMovement(ServerEntity entity, float deltaTime)
    {
      if (entity.IsBuilding) return;

      // Try to acquire target if none
      if (entity.Target == null || !entity.Target.IsAlive)
      {
        entity.Target = AcquireTarget(entity);
      }

      // Stop if in attack range
      if (entity.Target != null && IsInRange(entity, entity.Target, entity.Stats.AttackRange))
      {
        return;
      }

      // Calculate movement direction
      Vector2 direction = Vector2.zero;

      if (entity.Target != null && entity.Target.IsAlive)
      {
        // Move toward target
        direction = (entity.Target.Position - entity.Position).normalized;
      }
      else
      {
        // Move forward along lane (Team1 goes +Y, Team2 goes -Y)
        direction = new Vector2(0, entity.Team == EntityTeam.Team1 ? 1 : -1);
      }

      entity.Position += direction * entity.Stats.MoveSpeed * deltaTime;
    }

    private void UpdateCombat(ServerEntity entity, float deltaTime)
    {
      if (entity.Target == null || !entity.Target.IsAlive)
        return;

      // Check if in attack range
      if (!IsInRange(entity, entity.Target, entity.Stats.AttackRange))
        return;

      // Update attack cooldown
      entity.AttackCooldown -= deltaTime;
      if (entity.AttackCooldown <= 0)
      {
        // Execute attack
        DealDamage(entity, entity.Target);
        entity.AttackCooldown = 1f / entity.Stats.AttackSpeed;
      }
    }

    private ServerEntity AcquireTarget(ServerEntity entity)
    {
      ServerEntity closest = null;
      float closestDistSq = entity.Stats.AggroRange * entity.Stats.AggroRange;

      foreach (var other in entities)
      {
        if (!other.IsAlive || other.Team == entity.Team)
          continue;

        float distSq = (other.Position - entity.Position).sqrMagnitude;
        if (distSq < closestDistSq)
        {
          closest = other;
          closestDistSq = distSq;
        }
      }

      return closest;
    }

    private bool IsInRange(ServerEntity entity, ServerEntity target, float range)
    {
      float distSq = (target.Position - entity.Position).sqrMagnitude;
      return distSq <= range * range;
    }

    private void DealDamage(ServerEntity attacker, ServerEntity target)
    {
      target.TakeDamage(attacker.Stats.AttackDamage);

      if (!target.IsAlive)
      {
        Debug.Log($"[Server] Entity {target.Id} ({target.Type}) died");
      }
    }

    public ServerEntity SpawnEntity(string type, Vector2 position, EntityTeam team, bool isBuilding = false)
    {
      var entity = new ServerEntity(nextEntityId++, type, position, team, isBuilding);
      entities.Add(entity);
      Debug.Log($"[Server] Spawned {type} (id={entity.Id}) at {position}");
      return entity;
    }

    // Get all entities (for state sync)
    public List<ServerEntity> GetEntities() => entities;

    // Get entities by team (for match end detection)
    public List<ServerEntity> GetEntitiesByTeam(EntityTeam team)
    {
      return entities.Where(e => e.Team == team && e.IsAlive).ToList();
    }

    // Check if entity exists
    public bool HasEntity(int entityId)
    {
      return entities.Any(e => e.Id == entityId && e.IsAlive);
    }

    // Clear all entities (for match restart)
    public void Clear()
    {
      entities.Clear();
      nextEntityId = 1;
    }
  }
}
