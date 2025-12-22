using UnityEngine;

/// <summary>
/// Minimal snapshot of an entity's authoritative state.
/// Designed for efficient network transmission.
/// </summary>
[System.Serializable]
public struct EntitySnapshot
{
  // Core identity
  public int id;

  // Position (quantized to cm precision saves bandwidth)
  public short x;  // cm
  public short y;  // cm

  // HP (normalized to byte 0-255)
  public byte hp;

  // State flags
  public bool isAlive;

  // Helper: Create snapshot from server entity
  public static EntitySnapshot FromServerEntity(ClashServer.ServerEntity entity)
  {
    return new EntitySnapshot
    {
      id = entity.Id,
      x = (short)(entity.Position.x * 100f),  // meters to cm
      y = (short)(entity.Position.y * 100f),
      hp = (byte)Mathf.Clamp((entity.Stats.CurrentHP / entity.Stats.MaxHP) * 255f, 0, 255),
      isAlive = entity.IsAlive
    };
  }

  // Helper: Convert back to world position
  public Vector3 ToWorldPosition()
  {
    return new Vector3(x / 100f, 0, y / 100f);
  }

  // Helper: Get normalized HP (0-1)
  public float GetNormalizedHP()
  {
    return hp / 255f;
  }
}
