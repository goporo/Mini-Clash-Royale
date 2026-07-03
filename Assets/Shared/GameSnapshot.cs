using System;
using System.Collections.Generic;
using System.Numerics;

namespace ClashShared
{
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

  }

  [Serializable]
  public struct EntitySnapshot
  {
    public int Id;
    public Vector2Data Position;
    public EntityTeam Team;
    public CardId Type;
    public float CurrentHP;
    public float MaxHP;
    public int TargetId; // -1 means no target
    public bool IsAlive;
    public bool IsBuilding;
    public bool IsSlowed;
    public bool IsDecaying; // building bleeding HP from lifetime decay (client suppresses hit flash)

    public EntitySnapshot(
      int id,
      Vector2Data position,
      EntityTeam team,
      CardId type,
      float currentHP,
      float maxHP,
      int targetId,
      bool isAlive,
      bool isBuilding,
      bool isSlowed = false,
      bool isDecaying = false)
    {
      Id = id;
      Position = position;
      Team = team;
      Type = type;
      CurrentHP = currentHP;
      MaxHP = maxHP;
      TargetId = targetId;
      IsAlive = isAlive;
      IsBuilding = isBuilding;
      IsSlowed = isSlowed;
      IsDecaying = isDecaying;
    }
  }

  [Serializable]
  public class FullSnapshot
  {
    public uint Tick;
    public float GameTime;
    public List<EntitySnapshot> Entities;

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

  [Serializable]
  public class DeltaSnapshot
  {
    public uint Tick;
    public uint BaseTick;
    public List<EntitySnapshot> SpawnedEntities;
    public List<int> DestroyedEntityIds;
    public List<EntitySnapshot> UpdatedEntities;

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
}
