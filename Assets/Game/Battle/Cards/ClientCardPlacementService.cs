using UnityEngine;
using ClashShared;

public static class ClientCardPlacementService
{
  private const float GRID_SIZE = ClientBoardState.GRID_SIZE;

  private struct RegionBounds
  {
    public float Left;
    public float Right;
    public float Bottom;
    public float Top;
    public float RiverBottom;
    public float RiverTop;
  }

  private static readonly RegionBounds bounds = new()
  {
    Left = -9f,
    Right = 9f,
    Bottom = -16f,
    Top = 16f,
    RiverBottom = -1f,
    RiverTop = 1f
  };

  public static bool TryGetPlacement(Vector3 worldPosition, PlacementRule rule, out Vector2 snappedPosition)
  {
    if (worldPosition == Vector3.zero || worldPosition.z < bounds.Bottom)
    {
      snappedPosition = default;
      return false;
    }

    snappedPosition = ValidateAndSnapPosition(new Vector2(worldPosition.x, worldPosition.z), rule);
    return true;
  }

  public static float BottomWorldY => bounds.Bottom;

  private static Vector2 ValidateAndSnapPosition(Vector2 pos, PlacementRule rule)
  {
    float minY = bounds.Bottom;
    float maxY = bounds.RiverBottom;
    if (rule == PlacementRule.Anywhere)
      maxY = bounds.Top;

    float wx = Mathf.Clamp(pos.x, bounds.Left, bounds.Right);
    float wy = Mathf.Clamp(pos.y, minY, maxY);

    int minCX = Mathf.FloorToInt((bounds.Left + GRID_SIZE * 0.5f) / GRID_SIZE);
    int maxCX = Mathf.FloorToInt((bounds.Right - GRID_SIZE * 0.5f) / GRID_SIZE);
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
