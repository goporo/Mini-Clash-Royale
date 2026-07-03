using System;
using System.Collections.Generic;
using System.Numerics;
using ClashShared;

namespace ClashServer
{
  public enum MovementKind { Ground, Air, Stationary }

  public enum TargetCategory { AnyEnemy, BuildingsOnly }

  public enum AttackKind { None, Melee, Projectile, SelfDestruct }

  [Flags]
  public enum TargetLayer
  {
    None = 0,
    Ground = 1 << 0,
    Air = 1 << 1,
    GroundAndAir = Ground | Air
  }

  public sealed class TargetingRule
  {
    public TargetCategory Category { get; init; }
    public TargetLayer Layers { get; init; }
    // How far the unit "sees" enemies for aggro (Clash Royale sight ≈ 5.5 tiles).
    // Effective sight never drops below attack range, so long-range buildings still work.
    public float SightRange { get; init; } = 5.5f;
  }

  public sealed class StatusOnHit
  {
    public StatusKind Kind { get; init; }
    public int DurationTicks { get; init; }
    public float Magnitude { get; init; }
    public float Radius { get; init; }
    public TargetLayer AffectedLayers { get; init; }
  }

  public sealed class AttackRule
  {
    public AttackKind Kind { get; init; }
    public DamagePattern DamagePattern { get; init; }
    public float Damage { get; init; }
    public float Range { get; init; }
    public int CooldownTicks { get; init; }
    public int WindupTicks { get; init; }
    public float SplashRadius { get; init; }
    public TargetLayer AffectedLayers { get; init; }
    public ProjectileType? ProjectileType { get; init; }
    public StatusOnHit OnHitStatus { get; init; }
  }

  public enum ProjectileType { Bomb }

  public interface IImpactEffect
  {
    void Apply(ImpactContext context);
  }

  public sealed class ImpactContext
  {
    public ServerEntity Attacker { get; init; }
    public Vector2 ImpactPosition { get; init; }
    public IReadOnlyList<ServerEntity> Candidates { get; init; }
    public List<PendingDamage> Results { get; init; }
  }

  public sealed class AreaDamageImpactEffect : IImpactEffect
  {
    public float Radius { get; init; }
    public TargetLayer AffectedLayers { get; init; }

    public void Apply(ImpactContext context)
    {
      float damage = context.Attacker.Definition.Attack.Damage;
      foreach (ServerEntity candidate in context.Candidates)
      {
        if (!candidate.IsAlive || candidate.Team == context.Attacker.Team)
          continue;
        if ((AffectedLayers & candidate.Layer) == 0)
          continue;
        float effectiveRadius = Radius + candidate.FootprintRadius;
        float distSq = (candidate.Position - context.ImpactPosition).LengthSquared();
        if (distSq > effectiveRadius * effectiveRadius)
          continue;
        context.Results.Add(new PendingDamage(context.Attacker, candidate, damage));
      }
    }
  }

  public sealed class ProjectileDefinition
  {
    public ProjectileType Type { get; init; }
    public float Speed { get; init; }
    public IReadOnlyList<IImpactEffect> ImpactEffects { get; init; } = Array.Empty<IImpactEffect>();
  }

  public readonly struct SpawnRequest
  {
    public readonly CardId EntityType;
    public readonly Vector2 Position;
    public readonly EntityTeam Team;

    public SpawnRequest(CardId entityType, Vector2 position, EntityTeam team)
    {
      EntityType = entityType;
      Position = position;
      Team = team;
    }
  }

  public interface IEntityEffect
  {
    void Apply(EntityEffectContext context);
  }

  public sealed class EntityEffectContext
  {
    public ServerEntity SourceEntity { get; init; }
    public IReadOnlyList<ServerEntity> AllEntities { get; init; }
    public List<SpawnRequest> SpawnRequests { get; } = new();
  }

  public sealed class AreaDamageEffect : IEntityEffect
  {
    public float Radius { get; init; }
    public float Damage { get; init; }
    public TargetLayer AffectedLayers { get; init; }

    public void Apply(EntityEffectContext context)
    {
      foreach (ServerEntity candidate in context.AllEntities)
      {
        if (!candidate.IsAlive || candidate.Team == context.SourceEntity.Team)
          continue;
        if ((AffectedLayers & candidate.Layer) == 0)
          continue;
        float effectiveRadius = Radius + candidate.FootprintRadius;
        if ((candidate.Position - context.SourceEntity.Position).LengthSquared() > effectiveRadius * effectiveRadius)
          continue;
        candidate.TakeDamage(Damage);
      }
    }
  }

  public sealed class AreaApplyStatusEffect : IEntityEffect
  {
    public float Radius { get; init; }
    public StatusKind Kind { get; init; }
    public int DurationTicks { get; init; }
    public float Magnitude { get; init; }
    public TargetLayer AffectedLayers { get; init; }

    public void Apply(EntityEffectContext context)
    {
      foreach (ServerEntity candidate in context.AllEntities)
      {
        if (!candidate.IsAlive || candidate.Team == context.SourceEntity.Team)
          continue;
        if ((AffectedLayers & candidate.Layer) == 0)
          continue;
        float effectiveRadius = Radius + candidate.FootprintRadius;
        if ((candidate.Position - context.SourceEntity.Position).LengthSquared() > effectiveRadius * effectiveRadius)
          continue;
        candidate.ApplyStatus(Kind, DurationTicks, Magnitude);
      }
    }
  }

  public sealed class SpawnEntitiesEffect : IEntityEffect
  {
    public CardId EntityType { get; init; }
    public int Count { get; init; }
    public float SpreadRadius { get; init; }

    public void Apply(EntityEffectContext context)
    {
      for (int i = 0; i < Count; i++)
      {
        float angle = i * (2f * MathF.PI / Count);
        Vector2 offset = new Vector2(MathF.Cos(angle) * SpreadRadius, MathF.Sin(angle) * SpreadRadius);
        context.SpawnRequests.Add(new SpawnRequest(EntityType, context.SourceEntity.Position + offset, context.SourceEntity.Team));
      }
    }
  }

  public readonly struct PendingEntityEffect
  {
    public readonly IEntityEffect Effect;
    public readonly ServerEntity Source;

    public PendingEntityEffect(IEntityEffect effect, ServerEntity source)
    {
      Effect = effect;
      Source = source;
    }
  }

  public sealed class ServerEntityDefinition
  {
    public CardId CardId { get; init; }
    public CardType CardType { get; init; }
    public EntityStats Stats { get; init; }
    public float FootprintRadius { get; init; }
    public float CollisionRadius { get; init; }
    public float PushWeight { get; init; }
    public MovementKind Movement { get; init; }
    public TargetingRule Targeting { get; init; }
    public AttackRule Attack { get; init; }
    public int LifetimeTicks { get; init; } // 0 = no decay; >0 = building dies after this many ticks
    public bool HasLifetime => LifetimeTicks > 0;
    public IReadOnlyList<IEntityEffect> SpawnEffects { get; init; } = Array.Empty<IEntityEffect>();
    public IReadOnlyList<IEntityEffect> SelfDestructEffects { get; init; } = Array.Empty<IEntityEffect>();
    public IReadOnlyList<IEntityEffect> DeathEffects { get; init; } = Array.Empty<IEntityEffect>();
    public bool IsBuilding => CardType == CardType.Building;
  }
}
