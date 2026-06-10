using System;
using System.Collections.Generic;
using System.Numerics;
using ClashShared;

namespace ClashServer
{
  public class ServerEntity
  {
    private bool deathHandled;
    private readonly List<ActiveStatusEffect> _activeEffects = new();

    public int Id { get; }
    public ServerEntityDefinition Definition { get; }
    public CardId Type { get; }
    public Vector2 Position { get; private set; }
    public EntityTeam Team { get; }
    public EntityStats Stats { get; private set; }
    public ServerEntity Target { get; private set; }
    public bool IsAlive { get; private set; }
    public int AttackCooldownTicks { get; private set; }
    public bool IsBuilding { get; }
    public float FootprintRadius { get; }
    public float CollisionRadius { get; }
    public float PushWeight { get; }
    public List<Vector2> Path { get; private set; }
    public uint PathRecalcTick { get; private set; }
    public TargetLayer Layer => Definition.Movement == MovementKind.Air ? TargetLayer.Air : TargetLayer.Ground;

    public bool IsSlowed
    {
      get
      {
        foreach (ActiveStatusEffect effect in _activeEffects)
          if (effect.Kind == StatusKind.Slow) return true;
        return false;
      }
    }

    public float EffectiveMovePerTick
    {
      get
      {
        float speed = Stats.MovePerTick;
        foreach (ActiveStatusEffect effect in _activeEffects)
          if (effect.Kind == StatusKind.Slow)
            speed *= (1f - effect.Magnitude);
        return speed;
      }
    }

    public ServerEntity(int id, ServerEntityDefinition definition, Vector2 position, EntityTeam team)
    {
      Definition = definition ?? throw new ArgumentNullException(nameof(definition));
      Id = id;
      Type = definition.CardId;
      Position = position;
      Team = team;
      IsBuilding = definition.IsBuilding;
      IsAlive = true;
      AttackCooldownTicks = 0;
      Stats = definition.Stats;
      FootprintRadius = definition.FootprintRadius;
      CollisionRadius = definition.CollisionRadius;
      PushWeight = definition.PushWeight;
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

    public void MoveTo(Vector2 position)
    {
      Position = position;
    }

    public void SetTarget(ServerEntity target)
    {
      if (ReferenceEquals(Target, target))
        return;

      Target = target;
      ClearPath();
    }

    public void SetPath(List<Vector2> path, uint pathRecalcTick)
    {
      Path = path;
      PathRecalcTick = pathRecalcTick;
    }

    public void ClearPath()
    {
      Path = null;
    }

    public void TickAttackCooldown()
    {
      if (AttackCooldownTicks > 0)
        AttackCooldownTicks--;
    }

    public void ResetAttackCooldown()
    {
      AttackCooldownTicks = Definition.Attack.CooldownTicks;
    }

    public void ApplyStatus(StatusKind kind, int durationTicks, float magnitude)
    {
      foreach (ActiveStatusEffect effect in _activeEffects)
      {
        if (effect.Kind != kind)
          continue;
        effect.RemainingTicks = Math.Max(effect.RemainingTicks, durationTicks);
        effect.Magnitude = Math.Max(effect.Magnitude, magnitude);
        return;
      }
      _activeEffects.Add(new ActiveStatusEffect { Kind = kind, RemainingTicks = durationTicks, Magnitude = magnitude });
    }

    public void TickStatusEffects()
    {
      for (int i = _activeEffects.Count - 1; i >= 0; i--)
      {
        _activeEffects[i].RemainingTicks--;
        if (_activeEffects[i].RemainingTicks <= 0)
          _activeEffects.RemoveAt(i);
      }
    }

    public void HandleSpawn(IReadOnlyList<ServerEntity> allEntities)
    {
      var context = new EntityEffectContext { SourceEntity = this, AllEntities = allEntities };
      foreach (IEntityEffect effect in Definition.SpawnEffects)
        effect.Apply(context);
    }

    public EntityEffectContext HandleDeath(IReadOnlyList<ServerEntity> allEntities)
    {
      if (deathHandled)
        return null;

      deathHandled = true;

      var context = new EntityEffectContext { SourceEntity = this, AllEntities = allEntities };
      foreach (IEntityEffect effect in Definition.DeathEffects)
        effect.Apply(context);
      return context;
    }
  }
}
