using System.Collections.Generic;
using UnityEngine;

public class Entity
{
  public event System.Action OnDeathEvent;

  // ─────────────────────────────────────────────────────────────
  // Basic Identity
  public int Id { get; private set; }
  public EntityTeam Team { get; private set; }
  public bool IsBuilding { get; private set; }
  public EntityConfigBase Config { get; private set; }

  // ─────────────────────────────────────────────────────────────
  // Runtime State
  public Vector3 Position { get; set; }
  public Entity Target { get; set; }                // << CURRENT LOCKED TARGET
  public EntityStats Stats { get; private set; }
  public bool IsAlive => Stats.CurrentHP > 0;

  // ─────────────────────────────────────────────────────────────
  // Behaviours
  private readonly List<IEntityBehaviour> behaviours = new List<IEntityBehaviour>();

  // ─────────────────────────────────────────────────────────────
  // Director reference (simulation world)
  private GameplayDirector director;
  public GameplayDirector Director => director;

  // ─────────────────────────────────────────────────────────────
  // Constructor
  public Entity(int id, Vector3 spawnPos, EntityStats stats, EntityConfigBase config,
                EntityTeam team, GameplayDirector director, bool isBuilding)
  {
    Id = id;
    Position = spawnPos;
    Stats = stats;
    Config = config;
    Team = team;
    IsBuilding = isBuilding;
    this.director = director;
  }

  // ─────────────────────────────────────────────────────────────
  // Tick → executed every simulation update
  public void Tick(float dt)
  {
    if (!IsAlive) return; // fail-safe
    for (int i = 0; i < behaviours.Count; i++)
      behaviours[i].Tick(this, dt);
  }

  // ─────────────────────────────────────────────────────────────
  // Behaviour Control
  public void AddBehaviour(IEntityBehaviour behaviour)
  {
    behaviours.Add(behaviour);
  }

  // ─────────────────────────────────────────────────────────────
  // Movement proxy (optional)
  public void MoveTo(Vector3 newPos)
  {
    Position = newPos;
  }

  // ─────────────────────────────────────────────────────────────
  // Combat + Damage
  public void TakeDamage(float amount)
  {
    Debug.Log($"Entity {Id} took {amount} damage.");
    Stats.CurrentHP -= amount;

    if (Stats.CurrentHP <= 0)
    {
      Stats.CurrentHP = 0;
      OnDeath();
    }
  }

  // ─────────────────────────────────────────────────────────────
  // Death cleanup + target reset for all attackers
  private void OnDeath()
  {
    // Remove entity from simulation world
    director.Remove(this);

    // Clear targeting references from other entities
    foreach (var e in director.Entities.Entities) // pooled lookup OK
    {
      if (e.Target == this)
        e.Target = null; // force retarget next tick
    }

    OnDeathEvent?.Invoke();
  }
}
