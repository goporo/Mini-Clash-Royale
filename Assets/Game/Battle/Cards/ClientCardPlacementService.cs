using UnityEngine;
using ClashShared;

public static class ClientCardPlacementService
{
  private const float GRID_SIZE = ClientBoardState.GRID_SIZE;

  // rawWorldPos — direct raycast hit from the camera (never moves, so this is
  //               always server world-space for Team1 and mirrored for Team2).
  // snappedWorldPos — server world-space position to send to the server.
  public static bool TryGetPlacement(Vector3 rawWorldPos, PlacementRule rule, out Vector2 snappedWorldPos)
  {
    if (rawWorldPos == Vector3.zero)
    {
      snappedWorldPos = default;
      return false;
    }

    // Mirror the hit point to the local player's half of the world.
    // For Team1: identity.  For Team2: negate X and Z.
    Vector2 worldXZ = LocalPlayerContext.ToWorld(new Vector2(rawWorldPos.x, rawWorldPos.z));

    // Deploy zone in server world-space for the local player.
    float minY = LocalPlayerContext.IsTeam2 ? BattleArena.RiverTop  : BattleArena.Bottom;
    float maxY = LocalPlayerContext.IsTeam2 ? BattleArena.Top       : BattleArena.RiverBottom;
    if (rule == PlacementRule.Anywhere)
    {
      minY = BattleArena.Bottom;
      maxY = BattleArena.Top;
    }

    // Reject drags outside the field entirely.
    if (worldXZ.y < BattleArena.Bottom || worldXZ.y > BattleArena.Top)
    {
      snappedWorldPos = default;
      return false;
    }

    snappedWorldPos = SnapToCell(worldXZ, minY, maxY, rule);
    return true;
  }

  private static Vector2 SnapToCell(Vector2 worldXZ, float minY, float maxY, PlacementRule rule)
  {
    float wx = Mathf.Clamp(worldXZ.x, BattleArena.Left, BattleArena.Right);
    float wy = Mathf.Clamp(worldXZ.y, minY, maxY);

    int minCX = Mathf.FloorToInt((BattleArena.Left  + GRID_SIZE * 0.5f) / GRID_SIZE);
    int maxCX = Mathf.FloorToInt((BattleArena.Right - GRID_SIZE * 0.5f) / GRID_SIZE);
    int minCY = Mathf.FloorToInt((minY + GRID_SIZE * 0.5f) / GRID_SIZE);
    int maxCY = Mathf.FloorToInt((maxY - GRID_SIZE * 0.5f) / GRID_SIZE);

    var (cx, cy) = ClientBoardState.WorldToCell(new Vector2(wx, wy));
    cx = Mathf.Clamp(cx, minCX, maxCX);
    cy = Mathf.Clamp(cy, minCY, maxCY);

    var (freeCX, freeCY) = FindNearestFreeCell(cx, cy, minCX, maxCX, minCY, maxCY, rule);
    return ClientBoardState.CellCenter(freeCX, freeCY);
  }

  private static (int x, int y) FindNearestFreeCell(
      int cx, int cy, int minCX, int maxCX, int minCY, int maxCY, PlacementRule rule)
  {
    bool isAnywhere = rule == PlacementRule.Anywhere;

    if (!ClientBoardState.IsCellOccupied(cx, cy) || isAnywhere)
      return (cx, cy);

    for (int r = 1; r <= 8; r++)
    {
      (int x, int y)? best = null;
      float bestDistSq = float.MaxValue;

      for (int dx = -r; dx <= r; dx++)
      {
        for (int dy = -r; dy <= r; dy++)
        {
          if (Mathf.Abs(dx) + Mathf.Abs(dy) != r)
            continue;

          int nx = cx + dx;
          int ny = cy + dy;
          if (nx < minCX || nx > maxCX || ny < minCY || ny > maxCY)
            continue;
          if (!isAnywhere && ClientBoardState.IsCellOccupied(nx, ny))
            continue;

          float distanceSq = dx * dx + dy * dy;
          if (distanceSq < bestDistSq)
          {
            bestDistSq = distanceSq;
            best = (nx, ny);
          }
        }
      }

      if (best.HasValue)
        return best.Value;
    }

    return (cx, cy);
  }
}
