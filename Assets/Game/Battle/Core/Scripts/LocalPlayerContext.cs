using ClashShared;
using UnityEngine;

public static class LocalPlayerContext
{
  public static EntityTeam LocalTeam { get; private set; } = EntityTeam.Team1;
  public static bool IsTeam2 => LocalTeam == EntityTeam.Team2;

  private static Vector3 _boardOffset = Vector3.zero;

  public static void Init(Transform boardTransform)
  {
    _boardOffset = boardTransform.position;
  }

  public static void SetTeam(EntityTeam team)
  {
    LocalTeam = team;
  }

  // Server world pos → visual scene pos
  public static Vector3 ToVisual(Vector3 worldPos)
  {
    if (!IsTeam2)
      return worldPos + _boardOffset;

    return new Vector3(-worldPos.x, worldPos.y, -worldPos.z) + _boardOffset;
  }

  // Raycast hit (scene pos) → server world pos
  public static Vector3 ToWorld(Vector3 visualPos)
  {
    Vector3 unoffset = visualPos - _boardOffset;
    if (!IsTeam2) return unoffset;
    return new Vector3(-unoffset.x, unoffset.y, -unoffset.z);
  }

  public static Vector2 ToWorld(Vector2 visualXZ)
  {
    Vector2 unoffset = new(visualXZ.x - _boardOffset.x, visualXZ.y - _boardOffset.z);
    if (!IsTeam2) return unoffset;
    return new Vector2(-unoffset.x, -unoffset.y);
  }
}
