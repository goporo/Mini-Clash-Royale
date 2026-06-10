using UnityEngine;
using ClashShared;

public static class ClientCardPlacementService
{
  private const float GRID_SIZE = ClientBoardState.GRID_SIZE;

  public static bool TryGetPlacement(Vector3 worldPosition, PlacementRule rule, out Vector2 snappedPosition)
  {
    if (worldPosition == Vector3.zero || worldPosition.z < BattleArena.Bottom)
    {
      snappedPosition = default;
      return false;
    }

    snappedPosition = ValidateAndSnapPosition(new Vector2(worldPosition.x, worldPosition.z), rule);
    return true;
  }

  public static float BottomWorldY => BattleArena.Bottom;

  private static Vector2 ValidateAndSnapPosition(Vector2 pos, PlacementRule rule)
  {
    float minY = BattleArena.Bottom;
    float maxY = BattleArena.RiverBottom;
    if (rule == PlacementRule.Anywhere)
      maxY = BattleArena.Top;

    float wx = Mathf.Clamp(pos.x, BattleArena.Left, BattleArena.Right);
    float wy = Mathf.Clamp(pos.y, minY, maxY);

    int minCX = Mathf.FloorToInt((BattleArena.Left + GRID_SIZE * 0.5f) / GRID_SIZE);
    int maxCX = Mathf.FloorToInt((BattleArena.Right - GRID_SIZE * 0.5f) / GRID_SIZE);
    int minCY = Mathf.FloorToInt((minY + GRID_SIZE * 0.5f) / GRID_SIZE);
    int maxCY = Mathf.FloorToInt((maxY - GRID_SIZE * 0.5f) / GRID_SIZE);

    var (cx, cy) = ClientBoardState.WorldToCell(new Vector2(wx, wy));
    cx = Mathf.Clamp(cx, minCX, maxCX);
    cy = Mathf.Clamp(cy, minCY, maxCY);

    var (freeCX, freeCY) = FindNearestFreeCell(cx, cy, minCX, maxCX, minCY, maxCY, rule);
    return ClientBoardState.CellCenter(freeCX, freeCY);
  }

  private static (int x, int y) FindNearestFreeCell(int cx, int cy, int minCX, int maxCX, int minCY, int maxCY, PlacementRule rule)
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
