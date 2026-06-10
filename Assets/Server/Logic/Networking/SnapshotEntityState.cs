using System;
using ClashShared;

namespace ClashServer
{
  internal class EntityState
  {
    public Vector2Data Position;
    public float CurrentHP;
    public int TargetId;
    public bool IsAlive;
    public bool IsSlowed;

    public EntityState(EntitySnapshot snapshot)
    {
      Position = snapshot.Position;
      CurrentHP = snapshot.CurrentHP;
      TargetId = snapshot.TargetId;
      IsAlive = snapshot.IsAlive;
      IsSlowed = snapshot.IsSlowed;
    }

    public bool HasChangedFrom(EntitySnapshot snapshot, float positionThreshold = 0.01f, float hpThreshold = 0.1f)
    {
      if (Math.Abs(CurrentHP - snapshot.CurrentHP) > hpThreshold)
        return true;

      if (TargetId != snapshot.TargetId)
        return true;

      float dx = Position.X - snapshot.Position.X;
      float dy = Position.Y - snapshot.Position.Y;
      if (dx * dx + dy * dy > positionThreshold * positionThreshold)
        return true;

      if (IsAlive != snapshot.IsAlive)
        return true;

      return IsSlowed != snapshot.IsSlowed;
    }
  }
}
