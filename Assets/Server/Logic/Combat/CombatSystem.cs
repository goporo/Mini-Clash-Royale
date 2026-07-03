using System.Collections.Generic;
using ClashShared;

namespace ClashServer
{
  public readonly struct PendingDamage
  {
    public PendingDamage(ServerEntity attacker, ServerEntity target, float damage)
    {
      Attacker = attacker;
      Target = target;
      Damage = damage;
    }

    public ServerEntity Attacker { get; }
    public ServerEntity Target { get; }
    public float Damage { get; }
  }

  public sealed class CombatSystem
  {
    private readonly TargetingSystem targetingSystem;

    public CombatSystem(TargetingSystem targetingSystem)
    {
      this.targetingSystem = targetingSystem;
    }

    public (List<PendingDamage> Damages, List<PendingStatus> Statuses, List<PendingEntityEffect> Effects) CollectPending(IReadOnlyList<ServerEntity> liveEntities)
    {
      var damages = new List<PendingDamage>();
      var statuses = new List<PendingStatus>();
      var effects = new List<PendingEntityEffect>();

      foreach (ServerEntity entity in liveEntities)
      {
        if (!entity.IsAlive)
          continue;

        // Hit speed keeps counting down while chasing, like Clash Royale.
        entity.TickAttackCooldown();

        AttackRule attack = entity.Definition.Attack;
        if (attack.Kind == AttackKind.None)
          continue;

        // Target selection already happened in TargetingSystem.UpdateTargets this tick;
        // combat only validates and executes.
        if (entity.Target == null || !entity.Target.IsAlive)
          continue;

        if (!targetingSystem.IsInRange(entity, entity.Target, attack.Range))
          continue;

        if (entity.AttackCooldownTicks > 0)
          continue;

        if (attack.Kind == AttackKind.SelfDestruct)
        {
          damages.Add(new PendingDamage(entity, entity, entity.Stats.CurrentHP));
          foreach (IEntityEffect effect in entity.Definition.SelfDestructEffects)
            effects.Add(new PendingEntityEffect(effect, entity));
          continue; // no cooldown reset — entity is dying
        }

        int beforeCount = damages.Count;
        CollectDamage(entity, entity.Target, liveEntities, damages, statuses);
        if (damages.Count == beforeCount)
          continue;

        entity.ResetAttackCooldown();
      }

      return (damages, statuses, effects);
    }

    private void CollectDamage(
      ServerEntity attacker,
      ServerEntity primaryTarget,
      IReadOnlyList<ServerEntity> candidates,
      List<PendingDamage> damages,
      List<PendingStatus> statuses)
    {
      AttackRule attack = attacker.Definition.Attack;
      switch (attack.DamagePattern)
      {
        case DamagePattern.SingleTarget:
          if (primaryTarget != null)
            damages.Add(new PendingDamage(attacker, primaryTarget, attack.Damage));
          break;

        case DamagePattern.RadiusAroundSelf:
          foreach (ServerEntity candidate in candidates)
          {
            if (!candidate.IsAlive || candidate.Team == attacker.Team)
              continue;
            if ((attack.AffectedLayers & candidate.Layer) == 0)
              continue;
            if (!targetingSystem.IsWithinRadius(attacker.Position, candidate, attack.SplashRadius))
              continue;
            damages.Add(new PendingDamage(attacker, candidate, attack.Damage));
          }
          break;

        case DamagePattern.RadiusAroundTarget:
          if (primaryTarget == null)
            break;
          foreach (ServerEntity candidate in candidates)
          {
            if (!candidate.IsAlive || candidate.Team == attacker.Team)
              continue;
            if ((attack.AffectedLayers & candidate.Layer) == 0)
              continue;
            if (!targetingSystem.IsWithinRadius(primaryTarget.Position, candidate, attack.SplashRadius))
              continue;
            damages.Add(new PendingDamage(attacker, candidate, attack.Damage));
          }
          break;
      }

      if (attack.OnHitStatus != null && primaryTarget != null)
        CollectOnHitStatuses(attacker, primaryTarget, candidates, attack.OnHitStatus, statuses);
    }

    private static void CollectOnHitStatuses(
      ServerEntity attacker,
      ServerEntity primaryTarget,
      IReadOnlyList<ServerEntity> candidates,
      StatusOnHit onHit,
      List<PendingStatus> statuses)
    {
      if (onHit.Radius <= 0f)
      {
        statuses.Add(new PendingStatus(primaryTarget, onHit.Kind, onHit.DurationTicks, onHit.Magnitude));
        return;
      }

      System.Numerics.Vector2 impactPos = primaryTarget.Position;
      foreach (ServerEntity candidate in candidates)
      {
        if (!candidate.IsAlive || candidate.Team == attacker.Team)
          continue;
        if ((onHit.AffectedLayers & candidate.Layer) == 0)
          continue;
        float effectiveRadius = onHit.Radius + candidate.FootprintRadius;
        if ((candidate.Position - impactPos).LengthSquared() > effectiveRadius * effectiveRadius)
          continue;
        statuses.Add(new PendingStatus(candidate, onHit.Kind, onHit.DurationTicks, onHit.Magnitude));
      }
    }
  }
}
