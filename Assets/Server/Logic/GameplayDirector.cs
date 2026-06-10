using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ClashShared;

namespace ClashServer
{
  public class GameplayDirector
  {
    private readonly List<ServerEntity> entities = new();
    private readonly Dictionary<int, EntityState> lastSnapshotState = new();
    private readonly ILogger logger;
    private readonly BoardManager boardManager;
    private readonly MovementSystem movementSystem;
    private readonly CombatSystem combatSystem;
    private readonly DamageResolver damageResolver;

    private int nextEntityId = 1;
    private uint currentTick = 0;
    private float gameTime = 0f;

    public GameplayDirector(BoardManager boardManager, ILogger logger = null)
    {
      this.boardManager = boardManager ?? throw new ArgumentNullException(nameof(boardManager));
      this.logger = logger ?? new ConsoleLogger();

      var targetingSystem = new TargetingSystem();
      movementSystem = new MovementSystem(boardManager, targetingSystem);
      combatSystem = new CombatSystem(targetingSystem);
      damageResolver = new DamageResolver(this.logger);
    }

    public void Update()
    {
      currentTick++;
      gameTime += ServerTickSettings.FixedDeltaTime;

      List<ServerEntity> liveEntities = entities
        .Where(entity => entity.IsAlive)
        .OrderBy(entity => entity.Id)
        .ToList();

      movementSystem.UpdatePositions(liveEntities, currentTick);
      var (pendingDamage, pendingStatuses, pendingEffects) = combatSystem.CollectPending(liveEntities);
      foreach (PendingEntityEffect pe in pendingEffects)
      {
        var ctx = new EntityEffectContext { SourceEntity = pe.Source, AllEntities = liveEntities };
        pe.Effect.Apply(ctx);
      }
      List<SpawnRequest> spawnRequests = damageResolver.ApplyPendingDamage(pendingDamage, liveEntities);
      foreach (PendingStatus ps in pendingStatuses)
        if (ps.Target.IsAlive)
          ps.Target.ApplyStatus(ps.Kind, ps.DurationTicks, ps.Magnitude);
      foreach (ServerEntity entity in liveEntities)
        entity.TickStatusEffects();
      foreach (SpawnRequest req in spawnRequests)
        SpawnEntity(req.EntityType, req.Position, req.Team);

      foreach (int buildingId in entities.Where(entity => !entity.IsAlive && entity.IsBuilding).Select(entity => entity.Id).ToList())
        boardManager.RemoveBuilding(buildingId);

      entities.RemoveAll(entity => !entity.IsAlive);
    }

    public ServerEntity SpawnEntity(CardId type, Vector2 position, EntityTeam team, bool isBuilding = false)
    {
      ServerEntityDefinition definition = ServerEntityCatalog.Get(type);
      var entity = new ServerEntity(nextEntityId++, definition, position, team);
      entities.Add(entity);
      entity.HandleSpawn(entities);
      logger.Log($"[Server] Spawned {type} (id={entity.Id}) at {position}");
      return entity;
    }

    public List<ServerEntity> SpawnCard(
      CardId type,
      Vector2 position,
      EntityTeam team,
      SpawnFormation formation = SpawnFormation.Single,
      bool isBuilding = false)
    {
      Vector2[] offsets = GetFormationOffsets(formation);
      var spawned = new List<ServerEntity>(offsets.Length);
      foreach (Vector2 offset in offsets)
        spawned.Add(SpawnEntity(type, position + offset, team, isBuilding));

      return spawned;
    }

    public void ApplySpellEffect(CardId spellCardId, Vector2 position, EntityTeam team)
    {
      damageResolver.ApplySpellEffect(spellCardId, position, team, entities);
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
      return entities.Where(entity => entity.Team == team && entity.IsAlive).ToList();
    }

    public bool HasEntity(int entityId)
    {
      return entities.Any(entity => entity.Id == entityId && entity.IsAlive);
    }

    public void Clear()
    {
      entities.Clear();
      nextEntityId = 1;
      currentTick = 0;
      gameTime = 0f;
      lastSnapshotState.Clear();
    }

    public FullSnapshot GenerateFullSnapshot()
    {
      var entitySnapshots = new List<EntitySnapshot>();

      foreach (ServerEntity entity in entities)
      {
        if (entity.IsAlive)
          entitySnapshots.Add(CreateEntitySnapshot(entity));
      }

      return new FullSnapshot(currentTick, gameTime, entitySnapshots);
    }

    public DeltaSnapshot GenerateDeltaSnapshot()
    {
      uint baseTick = currentTick - 1;
      var delta = new DeltaSnapshot(currentTick, baseTick);
      var currentEntityIds = new HashSet<int>();

      foreach (ServerEntity entity in entities)
      {
        if (!entity.IsAlive)
          continue;

        currentEntityIds.Add(entity.Id);
        EntitySnapshot snapshot = CreateEntitySnapshot(entity);

        if (!lastSnapshotState.ContainsKey(entity.Id))
        {
          delta.SpawnedEntities.Add(snapshot);
          lastSnapshotState[entity.Id] = new EntityState(snapshot);
        }
        else
        {
          EntityState lastState = lastSnapshotState[entity.Id];
          if (lastState.HasChangedFrom(snapshot))
          {
            delta.UpdatedEntities.Add(snapshot);
            lastSnapshotState[entity.Id] = new EntityState(snapshot);
          }
        }
      }

      List<int> destroyedIds = lastSnapshotState.Keys.Where(id => !currentEntityIds.Contains(id)).ToList();
      foreach (int id in destroyedIds)
      {
        delta.DestroyedEntityIds.Add(id);
        lastSnapshotState.Remove(id);
      }

      return delta;
    }

    public void ResetSnapshotTracking()
    {
      lastSnapshotState.Clear();

      foreach (ServerEntity entity in entities)
      {
        if (entity.IsAlive)
        {
          EntitySnapshot snapshot = CreateEntitySnapshot(entity);
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
        entity.IsBuilding,
        entity.IsSlowed);
    }

    public uint CurrentTick => currentTick;
    public float GameTime => gameTime;
  }
}
