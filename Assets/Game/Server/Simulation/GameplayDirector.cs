using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ClashServer
{
  // Pure server simulation class
  public class GameplayDirector
  {
    private List<ServerEntity> entities = new List<ServerEntity>();
    private int nextEntityId = 1;

    private uint currentTick = 0;
    private float gameTime = 0f;
    private Dictionary<int, EntityState> lastSnapshotState = new Dictionary<int, EntityState>();

    private ILogger logger;

    public GameplayDirector(ILogger logger = null)
    {
      this.logger = logger ?? new ConsoleLogger();
    }

    // Main update loop - called every server tick
    public void Update(float deltaTime)
    {
      currentTick++;
      gameTime += deltaTime;

      // Phase 1: Update movement for all entities
      foreach (var entity in entities.ToList())
      {
        if (!entity.IsAlive) continue;
        UpdateMovement(entity, deltaTime);
      }

      // Phase 2: Calculate all attacks (without applying damage yet)
      List<(ServerEntity attacker, ServerEntity target, float damage)> pendingAttacks = new List<(ServerEntity, ServerEntity, float)>();

      foreach (var entity in entities.ToList())
      {
        if (!entity.IsAlive) continue;
        CalculateAttack(entity, deltaTime, pendingAttacks);
      }

      // Phase 3: Apply all damage simultaneously
      foreach (var (attacker, target, damage) in pendingAttacks)
      {
        if (target.IsAlive)
        {
          target.TakeDamage(damage);
          if (!target.IsAlive)
          {
            logger.Log($"[Server] Entity {target.Id} ({target.Type}) died");
          }
        }
      }

      entities.RemoveAll(e => !e.IsAlive);
    }

    private void UpdateMovement(ServerEntity entity, float deltaTime)
    {
      // Try to acquire target if none (even for buildings)
      if (entity.Target == null || !entity.Target.IsAlive)
      {
        entity.Target = AcquireTarget(entity);
      }

      if (entity.IsBuilding) return;

      if (entity.Target != null && IsInRange(entity, entity.Target, entity.Stats.AttackRange))
      {
        return;
      }

      Vector2 direction = Vector2.Zero;

      if (entity.Target != null && entity.Target.IsAlive)
      {
        direction = Vector2.Normalize(entity.Target.Position - entity.Position);
      }
      else
      {
        direction = new Vector2(0, entity.Team == EntityTeam.Team1 ? 1 : -1);
      }

      entity.Position += direction * entity.Stats.MoveSpeed * deltaTime;
    }

    private void CalculateAttack(ServerEntity entity, float deltaTime, List<(ServerEntity attacker, ServerEntity target, float damage)> pendingAttacks)
    {
      if (entity.Target == null || !entity.Target.IsAlive)
        return;

      if (!IsInRange(entity, entity.Target, entity.Stats.AttackRange))
        return;

      entity.AttackCooldown -= deltaTime;
      if (entity.AttackCooldown <= 0)
      {
        pendingAttacks.Add((entity, entity.Target, entity.Stats.AttackDamage));
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

        float distSq = (other.Position - entity.Position).LengthSquared();
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
      float distSq = (target.Position - entity.Position).LengthSquared();
      return distSq <= range * range;
    }

    public ServerEntity SpawnEntity(string type, Vector2 position, EntityTeam team, bool isBuilding = false)
    {
      var entity = new ServerEntity(nextEntityId++, type, position, team, isBuilding);
      entities.Add(entity);
      logger.Log($"[Server] Spawned {type} (id={entity.Id}) at {position}");
      return entity;
    }

    public List<ServerEntity> GetEntities() => entities;

    public List<ServerEntity> GetEntitiesByTeam(EntityTeam team)
    {
      return entities.Where(e => e.Team == team && e.IsAlive).ToList();
    }

    public bool HasEntity(int entityId)
    {
      return entities.Any(e => e.Id == entityId && e.IsAlive);
    }

    public void Clear()
    {
      entities.Clear();
      nextEntityId = 1;
      currentTick = 0;
      gameTime = 0f;
      lastSnapshotState.Clear();
    }

    // === SNAPSHOT SYSTEM ===

    /// <summary>
    /// Generate a full snapshot of the current game state.
    /// Use this for player reconnection or initial connection.
    /// </summary>
    public FullSnapshot GenerateFullSnapshot()
    {
      var entitySnapshots = new List<EntitySnapshot>();

      foreach (var entity in entities)
      {
        if (entity.IsAlive)
        {
          entitySnapshots.Add(new EntitySnapshot(entity));
        }
      }

      return new FullSnapshot(currentTick, gameTime, entitySnapshots);
    }

    /// <summary>
    /// Generate a delta snapshot containing only changes since the last delta was generated.
    /// Use this for regular tick updates to minimize bandwidth.
    /// </summary>
    public DeltaSnapshot GenerateDeltaSnapshot()
    {
      uint baseTick = currentTick - 1;
      var delta = new DeltaSnapshot(currentTick, baseTick);
      var currentEntityIds = new HashSet<int>();

      foreach (var entity in entities)
      {
        if (!entity.IsAlive) continue;

        currentEntityIds.Add(entity.Id);
        var snapshot = new EntitySnapshot(entity);

        if (!lastSnapshotState.ContainsKey(entity.Id))
        {
          delta.SpawnedEntities.Add(snapshot);
          lastSnapshotState[entity.Id] = new EntityState(snapshot);
        }
        else
        {
          var lastState = lastSnapshotState[entity.Id];
          if (lastState.HasChangedFrom(snapshot))
          {
            delta.UpdatedEntities.Add(snapshot);
            lastSnapshotState[entity.Id] = new EntityState(snapshot);
          }
        }
      }

      var destroyedIds = lastSnapshotState.Keys.Where(id => !currentEntityIds.Contains(id)).ToList();
      foreach (var id in destroyedIds)
      {
        delta.DestroyedEntityIds.Add(id);
        lastSnapshotState.Remove(id);
      }

      return delta;
    }

    /// <summary>
    /// Reset the snapshot tracking state. Call this after sending a full snapshot
    /// to ensure the next delta is calculated correctly.
    /// </summary>
    public void ResetSnapshotTracking()
    {
      lastSnapshotState.Clear();

      // Initialize tracking with current state
      foreach (var entity in entities)
      {
        if (entity.IsAlive)
        {
          var snapshot = new EntitySnapshot(entity);
          lastSnapshotState[entity.Id] = new EntityState(snapshot);
        }
      }
    }

    public uint CurrentTick => currentTick;
    public float GameTime => gameTime;
  }
}
