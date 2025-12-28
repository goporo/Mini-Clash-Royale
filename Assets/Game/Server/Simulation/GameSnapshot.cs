using System;
using System.Collections.Generic;
using System.Numerics;

namespace ClashServer
{
  // Pure server snapshot class

  [Serializable]
  public struct Vector2Data
  {
    public float X;
    public float Y;

    public Vector2Data(float x, float y)
    {
      X = x;
      Y = y;
    }

    public static Vector2Data FromVector2(Vector2 v)
    {
      return new Vector2Data(v.X, v.Y);
    }

    public Vector2 ToVector2()
    {
      return new Vector2(X, Y);
    }

#if UNITY_5_3_OR_NEWER
    // Unity-specific conversions for client code
    public static Vector2Data FromUnityVector2(UnityEngine.Vector2 v)
    {
      return new Vector2Data(v.x, v.y);
    }

    public UnityEngine.Vector2 ToUnityVector2()
    {
      return new UnityEngine.Vector2(X, Y);
    }
#endif
  }

  // Snapshot of a single entity's state
  [Serializable]
  public struct EntitySnapshot
  {
    public int Id;
    public Vector2Data Position;
    public EntityTeam Team;
    public string Type;
    public float CurrentHP;
    public float MaxHP;
    public int TargetId; // -1 means no target
    public bool IsAlive;
    public bool IsBuilding;

    public EntitySnapshot(ServerEntity entity)
    {
      Id = entity.Id;
      Position = Vector2Data.FromVector2(entity.Position);
      Team = entity.Team;
      Type = entity.Type;
      CurrentHP = entity.Stats.CurrentHP;
      MaxHP = entity.Stats.MaxHP;
      TargetId = entity.Target?.Id ?? -1;
      IsAlive = entity.IsAlive;
      IsBuilding = entity.IsBuilding;
    }
  }

  // Full game state snapshot - sent on reconnect or initial connection
  [Serializable]
  public class FullSnapshot
  {
    public uint Tick;
    public float GameTime;
    public List<EntitySnapshot> Entities;

    // Parameterless constructor for Mirror serialization
    public FullSnapshot()
    {
      Tick = 0;
      GameTime = 0f;
      Entities = new List<EntitySnapshot>();
    }

    public FullSnapshot(uint tick, float gameTime, List<EntitySnapshot> entities)
    {
      Tick = tick;
      GameTime = gameTime;
      Entities = entities ?? new List<EntitySnapshot>();
    }
  }

  // Delta snapshot - only contains changes since last snapshot
  [Serializable]
  public class DeltaSnapshot
  {
    public uint Tick;
    public uint BaseTick; // The tick this delta is based on

    // Entities that were spawned since last snapshot
    public List<EntitySnapshot> SpawnedEntities;

    // Entities that died since last snapshot
    public List<int> DestroyedEntityIds;

    // Entities with updated state (position, HP, target)
    public List<EntitySnapshot> UpdatedEntities;

    // Parameterless constructor for Mirror serialization
    public DeltaSnapshot()
    {
      Tick = 0;
      BaseTick = 0;
      SpawnedEntities = new List<EntitySnapshot>();
      DestroyedEntityIds = new List<int>();
      UpdatedEntities = new List<EntitySnapshot>();
    }

    public DeltaSnapshot(uint tick, uint baseTick)
    {
      Tick = tick;
      BaseTick = baseTick;
      SpawnedEntities = new List<EntitySnapshot>();
      DestroyedEntityIds = new List<int>();
      UpdatedEntities = new List<EntitySnapshot>();
    }
  }

  // Helper to track entity changes for delta generation
  internal class EntityState
  {
    public Vector2Data Position;
    public float CurrentHP;
    public int TargetId; // -1 means no target
    public bool IsAlive;

    public EntityState(EntitySnapshot snapshot)
    {
      Position = snapshot.Position;
      CurrentHP = snapshot.CurrentHP;
      TargetId = snapshot.TargetId;
      IsAlive = snapshot.IsAlive;
    }

    // Check if entity has changed significantly (for delta snapshots)
    public bool HasChangedFrom(EntitySnapshot snapshot, float positionThreshold = 0.01f, float hpThreshold = 0.1f)
    {
      // HP changed
      if (Math.Abs(CurrentHP - snapshot.CurrentHP) > hpThreshold)
        return true;

      // Target changed
      if (TargetId != snapshot.TargetId)
        return true;

      // Position changed (with threshold to avoid sending micro-movements)
      float dx = Position.X - snapshot.Position.X;
      float dy = Position.Y - snapshot.Position.Y;
      if (dx * dx + dy * dy > positionThreshold * positionThreshold)
        return true;

      // Alive state changed
      if (IsAlive != snapshot.IsAlive)
        return true;

      return false;
    }
  }
}
