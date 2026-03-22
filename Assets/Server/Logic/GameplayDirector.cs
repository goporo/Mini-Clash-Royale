using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ClashShared;

namespace ClashServer
{
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
    private const int MOVEMENT_RESOLVE_ITERATIONS = 6;
    private const float WAYPOINT_REACHED_EPSILON = 0.05f;
    private const float OVERLAP_EPSILON = 0.001f;

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
      gameTime += ServerTickSettings.FixedDeltaTime;

      var liveEntities = entities
        .Where(e => e.IsAlive)
        .OrderBy(e => e.Id)
        .ToList();

      // Phase 1: Resolve troop movement from desired positions, then separate overlaps.
      var desiredPositions = new Dictionary<int, Vector2>(liveEntities.Count);
      foreach (var entity in liveEntities)
        desiredPositions[entity.Id] = GetDesiredPosition(entity);

      ResolveMovement(liveEntities, desiredPositions);

      // Phase 2: Calculate all attacks (without applying damage yet)
      List<(ServerEntity attacker, ServerEntity target, float damage)> pendingAttacks = new List<(ServerEntity, ServerEntity, float)>();

      foreach (var entity in liveEntities)
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

      if (boardManager != null)
      {
        foreach (var buildingId in entities.Where(e => !e.IsAlive && e.IsBuilding).Select(e => e.Id).ToList())
          boardManager.RemoveBuilding(buildingId);
      }

      entities.RemoveAll(e => !e.IsAlive);
    }

    private Vector2 GetDesiredPosition(ServerEntity entity)
    {
      var newTarget = (entity.Target == null || !entity.Target.IsAlive)
        ? AcquireTarget(entity)
        : entity.Target;

      // Clear cached path whenever the target changes
      if (newTarget?.Id != entity.Target?.Id)
      {
        entity.Path = null;
        entity.Target = newTarget;
      }

      if (entity.IsBuilding || entity.Stats.MovePerTick <= 0f)
        return entity.Position;

      // Stop moving once in attack range
      if (entity.Target != null && IsInRange(entity, entity.Target, entity.Stats.AttackRange))
      {
        entity.Path = null;
        return entity.Position;
      }

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
        return MoveTowards(entity.Position, next, entity.Stats.MovePerTick);
      }

      // Only use direct movement when pathfinding is unavailable.
      if (boardManager != null)
        return entity.Position;

      Vector2 dir = goal - entity.Position;
      float len = dir.Length();
      if (len > 0)
        return entity.Position + (dir / len) * entity.Stats.MovePerTick;

      return entity.Position;
    }

    private void ResolveMovement(List<ServerEntity> liveEntities, Dictionary<int, Vector2> desiredPositions)
    {
      var mobileEntities = liveEntities
        .Where(e => !e.IsBuilding && e.CollisionRadius > 0f)
        .ToList();

      if (mobileEntities.Count == 0)
        return;

      var buildings = liveEntities
        .Where(e => e.IsBuilding && e.CollisionRadius > 0f)
        .ToList();

      var resolvedPositions = new Dictionary<int, Vector2>(mobileEntities.Count);
      foreach (var entity in mobileEntities)
        resolvedPositions[entity.Id] = ClampToArena(desiredPositions[entity.Id], entity.CollisionRadius);

      for (int iteration = 0; iteration < MOVEMENT_RESOLVE_ITERATIONS; iteration++)
      {
        bool movedAny = false;

        foreach (var entity in mobileEntities)
          movedAny |= ResolveStaticCollisions(entity, resolvedPositions, buildings);

        for (int i = 0; i < mobileEntities.Count; i++)
        {
          for (int j = i + 1; j < mobileEntities.Count; j++)
            movedAny |= ResolveUnitOverlap(
              mobileEntities[i],
              mobileEntities[j],
              resolvedPositions,
              desiredPositions);
        }

        if (!movedAny)
          break;
      }

      foreach (var entity in mobileEntities)
      {
        entity.Position = KeepOnWalkableCell(
          entity,
          ClampToArena(resolvedPositions[entity.Id], entity.CollisionRadius));

        AdvancePathProgress(entity);
      }
    }

    private bool ResolveStaticCollisions(
      ServerEntity entity,
      Dictionary<int, Vector2> resolvedPositions,
      IReadOnlyList<ServerEntity> buildings)
    {
      Vector2 original = resolvedPositions[entity.Id];
      Vector2 adjusted = original;

      foreach (var building in buildings)
      {
        float minDistance = entity.CollisionRadius + building.CollisionRadius;
        Vector2 delta = adjusted - building.Position;
        float distanceSq = delta.LengthSquared();

        if (distanceSq >= minDistance * minDistance)
          continue;

        float distance = MathF.Sqrt(distanceSq);
        Vector2 normal = distance > OVERLAP_EPSILON
          ? delta / distance
          : GetDeterministicSeparationDirection(entity.Id, building.Id);

        adjusted = building.Position + normal * (minDistance + OVERLAP_EPSILON);
      }

      adjusted = KeepOnWalkableCell(entity, ClampToArena(adjusted, entity.CollisionRadius));
      if ((adjusted - original).LengthSquared() <= OVERLAP_EPSILON * OVERLAP_EPSILON)
        return false;

      resolvedPositions[entity.Id] = adjusted;
      return true;
    }

    private bool ResolveUnitOverlap(
      ServerEntity a,
      ServerEntity b,
      Dictionary<int, Vector2> resolvedPositions,
      Dictionary<int, Vector2> desiredPositions)
    {
      Vector2 aPos = resolvedPositions[a.Id];
      Vector2 bPos = resolvedPositions[b.Id];
      float minDistance = a.CollisionRadius + b.CollisionRadius;
      Vector2 delta = bPos - aPos;
      float distanceSq = delta.LengthSquared();

      if (distanceSq >= minDistance * minDistance)
        return false;

      float distance = MathF.Sqrt(distanceSq);
      Vector2 normal = distance > OVERLAP_EPSILON
        ? delta / distance
        : GetDeterministicSeparationDirection(a.Id, b.Id);

      float overlap = minDistance - distance + OVERLAP_EPSILON;
      float aDisplacementShare = GetDisplacementShare(a, desiredPositions[a.Id], b, desiredPositions[b.Id]);
      float bDisplacementShare = 1f - aDisplacementShare;

      aPos -= normal * overlap * aDisplacementShare;
      bPos += normal * overlap * bDisplacementShare;

      resolvedPositions[a.Id] = ClampToArena(aPos, a.CollisionRadius);
      resolvedPositions[b.Id] = ClampToArena(bPos, b.CollisionRadius);
      return true;
    }

    private float GetDisplacementShare(
      ServerEntity entity,
      Vector2 desiredPosition,
      ServerEntity other,
      Vector2 otherDesiredPosition)
    {
      float entityResistance = GetPushResistance(entity, desiredPosition);
      float otherResistance = GetPushResistance(other, otherDesiredPosition);
      float totalResistance = entityResistance + otherResistance;

      if (totalResistance <= 0f)
        return 0.5f;

      return otherResistance / totalResistance;
    }

    private bool HasMovementIntent(ServerEntity entity, Vector2 desiredPosition)
    {
      return (desiredPosition - entity.Position).LengthSquared() > OVERLAP_EPSILON * OVERLAP_EPSILON;
    }

    private float GetPushResistance(ServerEntity entity, Vector2 desiredPosition)
    {
      float resistance = MathF.Max(entity.PushWeight, 0.1f);

      if (!HasMovementIntent(entity, desiredPosition))
        resistance *= 1.25f;

      return resistance;
    }

    private void AdvancePathProgress(ServerEntity entity)
    {
      while (entity.Path != null &&
             entity.Path.Count > 0 &&
             (entity.Position - entity.Path[0]).LengthSquared() <= WAYPOINT_REACHED_EPSILON * WAYPOINT_REACHED_EPSILON)
      {
        entity.Path.RemoveAt(0);
      }
    }

    private Vector2 KeepOnWalkableCell(ServerEntity entity, Vector2 position)
    {
      if (boardManager == null)
        return position;

      var (cx, cy) = NavGrid.WorldToCell(position);
      if (NavGrid.IsWalkable(cx, cy, boardManager))
        return position;

      Vector2 fallback = ClampToArena(entity.Position, entity.CollisionRadius);
      var (fx, fy) = NavGrid.WorldToCell(fallback);
      return NavGrid.IsWalkable(fx, fy, boardManager) ? fallback : position;
    }

    private static Vector2 MoveTowards(Vector2 from, Vector2 to, float maxDistance)
    {
      Vector2 delta = to - from;
      float distance = delta.Length();

      if (distance <= maxDistance || distance <= OVERLAP_EPSILON)
        return to;

      return from + delta / distance * maxDistance;
    }

    private static Vector2 ClampToArena(Vector2 position, float radius)
    {
      return new Vector2(
        Math.Clamp(position.X, NavGrid.WORLD_LEFT + radius, NavGrid.WORLD_RIGHT - radius),
        Math.Clamp(position.Y, NavGrid.WORLD_BOTTOM + radius, NavGrid.WORLD_TOP - radius));
    }

    private static Vector2 GetDeterministicSeparationDirection(int firstId, int secondId)
    {
      uint hash = (uint)(firstId * 73856093 ^ secondId * 19349663);
      float angle = (hash & 1023) / 1024f * MathF.PI * 2f;
      return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
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
      switch (spellCardId)
      {
        case CardId.Fireball:
          float fireballRadius = 2.5f;
          float fireballDamage = 50f;
          foreach (var entity in entities)
          {
            if (entity.IsAlive && (entity.Position - position).Length() <= fireballRadius + entity.FootprintRadius && entity.Team != team)
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
          entitySnapshots.Add(CreateEntitySnapshot(entity));
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
        var snapshot = CreateEntitySnapshot(entity);

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

      foreach (var entity in entities)
      {
        if (entity.IsAlive)
        {
          var snapshot = CreateEntitySnapshot(entity);
          lastSnapshotState[entity.Id] = new EntityState(snapshot);
        }
      }
    }

    private static EntitySnapshot CreateEntitySnapshot(ServerEntity entity)
    {
      return new EntitySnapshot(
        entity.Id,
        Vector2Data.FromVector2(entity.Position),
        entity.Team,
        entity.Type,
        entity.Stats.CurrentHP,
        entity.Stats.MaxHP,
        entity.Target?.Id ?? -1,
        entity.IsAlive,
        entity.IsBuilding);
    }

    public uint CurrentTick => currentTick;
    public float GameTime => gameTime;
  }
}
