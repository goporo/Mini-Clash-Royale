using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ClashShared;

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
    private BoardManager boardManager;

    private const int PATH_RECALC_INTERVAL = 3; // ticks between path refreshes

    public GameplayDirector(ILogger logger = null)
    {
      this.logger = logger ?? new ConsoleLogger();
    }

    /// <summary>
    /// Provide the shared BoardManager so pathfinding can query building occupancy.
    /// Call this once after construction (e.g. from ServerMatchController).
    /// </summary>
    public void SetBoardManager(BoardManager bm) => boardManager = bm;

    public void Update()
    {
      currentTick++;
      gameTime += ServerMatchController.FIXED_DT;

      // Phase 1: Update movement for all entities (deterministic order)
      foreach (var entity in entities.OrderBy(e => e.Id).ToList())
      {
        if (!entity.IsAlive) continue;
        UpdateMovement(entity);
      }

      // Phase 2: Calculate all attacks (without applying damage yet)
      List<(ServerEntity attacker, ServerEntity target, float damage)> pendingAttacks = new List<(ServerEntity, ServerEntity, float)>();

      foreach (var entity in entities.OrderBy(e => e.Id).ToList())
      {
        if (!entity.IsAlive) continue;
        CalculateAttack(entity, pendingAttacks);
      }

      // Phase 3: Apply all damage simultaneously
      foreach (var (attacker, target, damage) in pendingAttacks)
      {
        if (target.IsAlive)
        {
          target.TakeDamage(damage);
          if (!target.IsAlive)
          {
            // logger.Log($"[Server] Entity {target.Id} ({target.Type}) died");
          }
        }
      }

      entities.RemoveAll(e => !e.IsAlive);
    }

    private void UpdateMovement(ServerEntity entity)
    {
      // Acquire / refresh target
      var newTarget = (entity.Target == null || !entity.Target.IsAlive)
        ? AcquireTarget(entity)
        : entity.Target;

      // Clear cached path whenever the target changes
      if (newTarget?.Id != entity.Target?.Id)
      {
        entity.Path = null;
        entity.Target = newTarget;
      }

      if (entity.IsBuilding) return;

      // Stop moving once in attack range
      if (entity.Target != null && IsInRange(entity, entity.Target, entity.Stats.AttackRange))
      {
        entity.Path = null;
        return;
      }

      // Choose movement goal
      Vector2 goal = entity.Target != null
        ? entity.Target.Position
        : new Vector2(entity.Position.X, entity.Team == EntityTeam.Team1 ? NavGrid.WORLD_TOP : NavGrid.WORLD_BOTTOM);

      // (Re-)compute path when due or missing
      if (boardManager != null &&
          (entity.Path == null || currentTick >= entity.PathRecalcTick))
      {
        entity.Path = Pathfinder.FindPath(entity.Position, goal, boardManager);
        entity.PathRecalcTick = currentTick + (uint)PATH_RECALC_INTERVAL;
      }

      // Move along path
      if (entity.Path != null && entity.Path.Count > 0)
      {
        Vector2 next = entity.Path[0];
        Vector2 toNext = next - entity.Position;
        float dist = toNext.Length();

        if (dist <= entity.Stats.MovePerTick)
        {
          entity.Position = next;
          entity.Path.RemoveAt(0);
        }
        else
        {
          entity.Position += toNext / dist * entity.Stats.MovePerTick;
        }
        return;
      }

      // Fallback: straight-line movement (no board manager or no path)
      Vector2 dir = goal - entity.Position;
      float len = dir.Length();
      if (len > 0)
        entity.Position += (dir / len) * entity.Stats.MovePerTick;
    }

    private void CalculateAttack(ServerEntity entity, List<(ServerEntity attacker, ServerEntity target, float damage)> pendingAttacks)
    {
      if (entity.Target == null || !entity.Target.IsAlive)
        return;

      if (!IsInRange(entity, entity.Target, entity.Stats.AttackRange))
        return;

      if (entity.AttackCooldownTicks > 0)
      {
        entity.AttackCooldownTicks--;
        return;
      }

      if (entity.Type == CardId.Giant && !entity.Target.IsBuilding)
      {
        return;
      }

      pendingAttacks.Add((entity, entity.Target, entity.Stats.AttackDamage));
      entity.AttackCooldownTicks = entity.Stats.AttackCooldownTicks;
    }

    private ServerEntity AcquireTarget(ServerEntity entity)
    {
      ServerEntity closest = null;
      float closestDistSq = float.MaxValue;

      // Sort by ID for deterministic iteration
      foreach (var other in entities.OrderBy(e => e.Id))
      {
        if (!other.IsAlive || other.Team == entity.Team)
          continue;

        if (entity.Type == CardId.Giant && !other.IsBuilding)
          continue;

        float effectiveAggroRange = entity.Stats.AggroRange + other.FootprintRadius;
        float distSq = (other.Position - entity.Position).LengthSquared();

        // Use strict less-than for deterministic behavior
        // When distances are equal, lower ID wins (because we iterate by ID)
        if (distSq < effectiveAggroRange * effectiveAggroRange && distSq < closestDistSq)
        {
          closest = other;
          closestDistSq = distSq;
        }
      }

      return closest;
    }

    private bool IsInRange(ServerEntity entity, ServerEntity target, float range)
    {
      float effectiveRange = range + target.FootprintRadius;
      float distSq = (target.Position - entity.Position).LengthSquared();
      return distSq <= effectiveRange * effectiveRange;
    }

    public ServerEntity SpawnEntity(CardId type, Vector2 position, EntityTeam team, bool isBuilding = false)
    {
      var entity = new ServerEntity(nextEntityId++, type, position, team, isBuilding);
      entities.Add(entity);
      logger.Log($"[Server] Spawned {type} (id={entity.Id}) at {position}");
      return entity;
    }

    /// <summary>
    /// Spawn one or more entities according to a formation pattern.
    /// Returns all spawned entities (count matches the formation).
    /// </summary>
    public List<ServerEntity> SpawnCard(CardId type, Vector2 position, EntityTeam team,
        SpawnFormation formation = SpawnFormation.Single, bool isBuilding = false)
    {
      var offsets = GetFormationOffsets(formation);
      var spawned = new List<ServerEntity>(offsets.Length);
      foreach (var offset in offsets)
        spawned.Add(SpawnEntity(type, position + offset, team, isBuilding));
      return spawned;
    }

    public void ApplySpellEffect(CardId spellCardId, Vector2 position, EntityTeam team)
    {
      // For simplicity, we hardcode the spell effects here.
      // In a real implementation, you'd likely have a more data-driven approach.

      switch (spellCardId)
      {
        case CardId.Fireball:
          // Deal higher damage in a smaller radius
          float fireballRadius = 2.5f;
          float fireballDamage = 50f;
          foreach (var entity in entities)
          {
            if (entity.IsAlive && (entity.Position - position).Length() <= fireballRadius + entity.FootprintRadius)
            {
              entity.TakeDamage(fireballDamage);
              logger.Log($"[Server] FireballSpell hit Entity {entity.Id} for {fireballDamage} damage");
            }
          }
          break;
      }
    }

    private static Vector2[] GetFormationOffsets(SpawnFormation formation) => formation switch
    {
      SpawnFormation.Single => new[] { Vector2.Zero },
      SpawnFormation.DuoLine => new[] { new Vector2(-0.5f, 0f), new Vector2(0.5f, 0f) },
      SpawnFormation.TrioTriangle => new[] { new Vector2(0f, 0.5f), new Vector2(-0.5f, -0.5f), new Vector2(0.5f, -0.5f) },
      SpawnFormation.QuadDiamond => new[] { new Vector2(0f, 0.7f), new Vector2(0.7f, 0f), new Vector2(0f, -0.7f), new Vector2(-0.7f, 0f) },
      SpawnFormation.QuadLine => new[] { new Vector2(-1.5f, 0f), new Vector2(-0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1.5f, 0f) },
      SpawnFormation.Square => new[] { new Vector2(-0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, -0.5f), new Vector2(-0.5f, -0.5f) },
      _ => new[] { Vector2.Zero }
    };

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
