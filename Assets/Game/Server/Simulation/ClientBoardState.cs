using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClashServer
{
  /// <summary>
  /// Client-side mirror of the server's building occupancy.
  /// Populated from full/delta snapshots so the drag preview can
  /// snap away from occupied tiles without a server round-trip.
  /// </summary>
  public static class ClientBoardState
  {
    // Must match the client snap grid in CardDragController.
    public const float GRID_SIZE = 1f;

    // Building footprint sizes in world units – must mirror BoardManager.buildingSizes.
    private static readonly Dictionary<string, (float w, float h)> buildingSizes =
        new(StringComparer.OrdinalIgnoreCase)
    {
      { "tower",     (3f, 3f) },
      { "kingtower", (4f, 4f) },
      { "block",     (1f, 1f) },
    };

    // Occupied tile set.
    private static readonly HashSet<(int x, int y)> occupiedCells = new();

    // Id → (position, type) so delta removals can unregister the right cells.
    private static readonly Dictionary<int, (Vector2 pos, string type)> buildingById = new();

    // ─── Public API ─────────────────────────────────────────────────────────

    /// <summary>Full rebuild from a snapshot (call on connect / reconnect).</summary>
    public static void RebuildFromSnapshot(FullSnapshot snapshot)
    {
      occupiedCells.Clear();
      buildingById.Clear();
      foreach (var e in snapshot.Entities)
        if (e.IsBuilding && e.IsAlive)
          Register(e.Id, e.Position.ToUnityVector2(), e.Type);
    }

    /// <summary>Register a building that just spawned (delta snapshot).</summary>
    public static void PlaceBuilding(EntitySnapshot entity)
    {
      if (!entity.IsBuilding || !entity.IsAlive) return;
      Register(entity.Id, entity.Position.ToUnityVector2(), entity.Type);
    }

    /// <summary>Unregister a building that was destroyed (delta snapshot).</summary>
    public static void RemoveBuildingById(int entityId)
    {
      if (!buildingById.TryGetValue(entityId, out var info)) return;
      buildingById.Remove(entityId);
      foreach (var cell in GetFootprintCells(info.pos, info.type))
        occupiedCells.Remove(cell);
    }

    /// <summary>True if cell (cx, cy) is occupied by a building.</summary>
    public static bool IsCellOccupied(int cx, int cy) =>
        occupiedCells.Contains((cx, cy));

    /// <summary>Grid cell that contains <paramref name="worldPos"/> (floor-division).</summary>
    public static (int x, int y) WorldToCell(Vector2 worldPos) =>
        ((int)Math.Floor(worldPos.x / GRID_SIZE),
         (int)Math.Floor(worldPos.y / GRID_SIZE));

    /// <summary>World-space center of cell (cx, cy).</summary>
    public static Vector2 CellCenter(int cx, int cy) =>
        new((cx + 0.5f) * GRID_SIZE, (cy + 0.5f) * GRID_SIZE);

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static void Register(int id, Vector2 pos, string type)
    {
      buildingById[id] = (pos, type);
      foreach (var cell in GetFootprintCells(pos, type))
        occupiedCells.Add(cell);
    }

    /// <summary>
    /// All cells whose area [C*G, (C+1)*G) overlaps the building footprint.
    /// Uses the same formula as BoardManager.GetFootprintCells.
    /// </summary>
    private static IEnumerable<(int x, int y)> GetFootprintCells(Vector2 center, string type)
    {
      var (w, h) = buildingSizes.TryGetValue(type, out var s) ? s : (1f, 1f);

      int x0 = Mathf.CeilToInt((center.x - w * 0.5f) / GRID_SIZE);
      int x1 = Mathf.CeilToInt((center.x + w * 0.5f) / GRID_SIZE) - 1;
      int y0 = Mathf.CeilToInt((center.y - h * 0.5f) / GRID_SIZE);
      int y1 = Mathf.CeilToInt((center.y + h * 0.5f) / GRID_SIZE) - 1;

      for (int cx = x0; cx <= x1; cx++)
        for (int cy = y0; cy <= y1; cy++)
          yield return (cx, cy);
    }
  }
}
