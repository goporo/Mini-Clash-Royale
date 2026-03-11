using System;
using System.Numerics;

namespace ClashServer
{
  /// <summary>
  /// Answers per-cell walkability queries for the game board.
  /// Grid resolution matches BoardManager.GRID_SIZE (0.5 m).
  ///
  /// Passability rules:
  ///   1. Cells outside the board bounds are blocked.
  ///   2. River cells (Y ∈ [RIVER_BOTTOM, RIVER_TOP]) are blocked unless
  ///      the cell's X centre falls inside a declared bridge corridor.
  ///   3. Cells covered by a live building (BoardManager) are blocked.
  /// </summary>
  public static class NavGrid
  {
    // Resolution – must equal BoardManager.GRID_SIZE
    public const float GRID = BoardManager.GRID_SIZE; // 0.5 m

    // ── Board extents ─────────────────────────────────────────────────────
    public const float WORLD_LEFT = -9f;
    public const float WORLD_RIGHT = 9f;
    public const float WORLD_BOTTOM = -16f;
    public const float WORLD_TOP = 16f;

    // ── River band ────────────────────────────────────────────────────────
    public const float RIVER_WORLD_BOTTOM = -1f;
    public const float RIVER_WORLD_TOP = 1f;

    // ── Bridge corridors (inclusive world-X ranges) ───────────────────────
    // Adjust these to match the visual bridge positions in the scene.
    // Troops can cross the river only within these X extents.
    private static readonly (float xMin, float xMax)[] BridgeXRanges =
    {
      (-6f, -5f), // left bridge
      ( 5f,  6f), // right bridge
    };

    // ── Precomputed cell bounds ───────────────────────────────────────────
    public static readonly int CX_MIN = CellCoord(WORLD_LEFT);
    public static readonly int CX_MAX = CellCoord(WORLD_RIGHT) - 1;
    public static readonly int CY_MIN = CellCoord(WORLD_BOTTOM);
    public static readonly int CY_MAX = CellCoord(WORLD_TOP) - 1;

    private static readonly int RIVER_CY_MIN = CellCoord(RIVER_WORLD_BOTTOM);
    private static readonly int RIVER_CY_MAX = CellCoord(RIVER_WORLD_TOP) - 1;

    // ── Coordinate helpers ────────────────────────────────────────────────

    /// <summary>Cell index for a single world-unit axis value.</summary>
    public static int CellCoord(float worldVal) =>
      (int)Math.Floor(worldVal / GRID);

    /// <summary>Cell (cx, cy) that contains <paramref name="pos"/>.</summary>
    public static (int cx, int cy) WorldToCell(Vector2 pos) =>
      (CellCoord(pos.X), CellCoord(pos.Y));

    /// <summary>World-space centre of cell (cx, cy).</summary>
    public static Vector2 CellCenter(int cx, int cy) =>
      new Vector2((cx + 0.5f) * GRID, (cy + 0.5f) * GRID);

    // ── Walkability ───────────────────────────────────────────────────────

    public static bool IsInBounds(int cx, int cy) =>
      cx >= CX_MIN && cx <= CX_MAX && cy >= CY_MIN && cy <= CY_MAX;

    /// <summary>
    /// Returns true when a troop may occupy cell (cx, cy).
    /// </summary>
    public static bool IsWalkable(int cx, int cy, BoardManager board)
    {
      if (!IsInBounds(cx, cy)) return false;

      // River – only passable at bridge corridors
      if (cy >= RIVER_CY_MIN && cy <= RIVER_CY_MAX && !IsOnBridge(cx))
        return false;

      // Building footprint
      return !board.IsTileOccupied(CellCenter(cx, cy));
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static bool IsOnBridge(int cx)
    {
      float xCenter = (cx + 0.5f) * GRID;
      foreach (var (xMin, xMax) in BridgeXRanges)
        if (xCenter >= xMin && xCenter < xMax)
          return true;
      return false;
    }
  }
}
