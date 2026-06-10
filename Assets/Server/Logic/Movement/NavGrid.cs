using System;
using System.Numerics;

namespace ClashServer
{
  public static class NavGrid
  {
    public const float GRID = BoardManager.GRID_SIZE;

    public const float WORLD_LEFT = -9f;
    public const float WORLD_RIGHT = 9f;
    public const float WORLD_BOTTOM = -16f;
    public const float WORLD_TOP = 16f;
    public const float RIVER_WORLD_BOTTOM = -1f;
    public const float RIVER_WORLD_TOP = 1f;
    private static readonly (float xMin, float xMax)[] BridgeXRanges =
    {
      (-6.5f, -4.5f),
      (4.5f, 6.5f),
    };

    public static readonly int CX_MIN = CellCoord(WORLD_LEFT);
    public static readonly int CX_MAX = CellCoord(WORLD_RIGHT) - 1;
    public static readonly int CY_MIN = CellCoord(WORLD_BOTTOM);
    public static readonly int CY_MAX = CellCoord(WORLD_TOP) - 1;

    private static readonly int RIVER_CY_MIN = CellCoord(RIVER_WORLD_BOTTOM);
    private static readonly int RIVER_CY_MAX = CellCoord(RIVER_WORLD_TOP) - 1;


    public static int CellCoord(float worldVal) =>
      (int)Math.Floor(worldVal / GRID);

    public static (int cx, int cy) WorldToCell(Vector2 pos) =>
      (CellCoord(pos.X), CellCoord(pos.Y));

    public static Vector2 CellCenter(int cx, int cy) =>
      new((cx + 0.5f) * GRID, (cy + 0.5f) * GRID);


    public static bool IsInBounds(int cx, int cy) =>
      cx >= CX_MIN && cx <= CX_MAX && cy >= CY_MIN && cy <= CY_MAX;


    public static bool IsWalkable(int cx, int cy, BoardManager board)
    {
      if (!IsInBounds(cx, cy)) return false;

      if (cy >= RIVER_CY_MIN && cy <= RIVER_CY_MAX && !IsOnBridge(cx))
        return false;

      return !board.IsTileOccupied(CellCenter(cx, cy));
    }

    private static bool IsOnBridge(int cx)
    {
      float xCenter = (cx + 0.5f) * GRID;
      foreach (var (xMin, xMax) in BridgeXRanges)
      {
        if (xCenter >= xMin && xCenter < xMax)
          return true;
      }

      return false;
    }
  }
}
