
using System;
using System.Collections.Generic;
using System.Numerics;
using ClashShared;

namespace ClashServer
{
  /// <summary>
  /// Tracks which buildings occupy which grid tiles on the board.
  /// Buildings register their full multi-tile footprint so any spawn
  /// point inside a building area is correctly blocked.
  /// </summary>
  public class BoardManager
  {
    public const float GRID_SIZE = 0.5f;

    // Building footprint sizes in world units (width × height).
    // Add new building types here as the card roster grows.
    private static readonly Dictionary<CardId, (float w, float h)> buildingSizes =
        new()
    {
      { CardId.PrincessTower, (3f, 3f) },
      { CardId.KingTower,     (4f, 4f) },
    };

    public static (float w, float h) GetBuildingSize(CardId type) =>
        buildingSizes.TryGetValue(type, out var s) ? s : (1f, 1f);

    // Grid cell → occupying entity (multiple cells can point to the same entity)
    private readonly Dictionary<(int x, int y), ServerEntity> buildingGrid = new();

    // Reverse lookup: entityId → all registered cells for O(1) removal
    private readonly Dictionary<int, List<(int x, int y)>> entityToCells = new();

    /// <summary>Register all footprint cells when a building is spawned.</summary>
    public void PlaceBuilding(ServerEntity entity)
    {
      var cells = GetFootprintCells(entity.Position, entity.Type);
      entityToCells[entity.Id] = cells;
      foreach (var cell in cells)
        buildingGrid[cell] = entity;
    }

    /// <summary>Unregister all footprint cells when a building is removed/dies.</summary>
    public void RemoveBuilding(int entityId)
    {
      if (!entityToCells.TryGetValue(entityId, out var cells))
        return;

      foreach (var cell in cells)
        buildingGrid.Remove(cell);
      entityToCells.Remove(entityId);
    }

    /// <summary>
    /// True if the cell that contains <paramref name="worldPos"/> is occupied
    /// by a live building.  Dead entries are lazily cleaned up.
    /// </summary>
    public bool IsTileOccupied(Vector2 worldPos)
    {
      var cell = WorldToCell(worldPos);
      if (!buildingGrid.TryGetValue(cell, out var entity))
        return false;

      if (!entity.IsAlive)
      {
        RemoveBuilding(entity.Id);
        return false;
      }

      return true;
    }

    /// <summary>Returns the live building whose footprint covers <paramref name="worldPos"/>, or null.</summary>
    public ServerEntity GetBuildingAt(Vector2 worldPos)
    {
      var cell = WorldToCell(worldPos);
      if (!buildingGrid.TryGetValue(cell, out var entity))
        return null;

      if (!entity.IsAlive)
      {
        RemoveBuilding(entity.Id);
        return null;
      }

      return entity;
    }

    /// <summary>All currently live buildings (de-duplicated across multi-cell entries).</summary>
    public IReadOnlyList<ServerEntity> GetAllBuildings()
    {
      var live = new List<ServerEntity>();
      var seen = new HashSet<int>();
      foreach (var entity in buildingGrid.Values)
        if (entity.IsAlive && seen.Add(entity.Id))
          live.Add(entity);
      return live;
    }

    /// <summary>Remove all registrations. Call on match reset.</summary>
    public void Clear()
    {
      buildingGrid.Clear();
      entityToCells.Clear();
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all grid cells whose area [C*G, (C+1)*G) overlaps the building
    /// footprint centered at <paramref name="center"/>.
    /// </summary>
    private static List<(int x, int y)> GetFootprintCells(Vector2 center, CardId type)
    {
      var (w, h) = GetBuildingSize(type);

      // All cells C where [C*G, (C+1)*G) intersects [center - size/2, center + size/2]
      int x0 = (int)Math.Ceiling((center.X - w * 0.5f) / GRID_SIZE);
      int x1 = (int)Math.Ceiling((center.X + w * 0.5f) / GRID_SIZE) - 1;
      int y0 = (int)Math.Ceiling((center.Y - h * 0.5f) / GRID_SIZE);
      int y1 = (int)Math.Ceiling((center.Y + h * 0.5f) / GRID_SIZE) - 1;

      var cells = new List<(int, int)>((x1 - x0 + 1) * (y1 - y0 + 1));
      for (int cx = x0; cx <= x1; cx++)
        for (int cy = y0; cy <= y1; cy++)
          cells.Add((cx, cy));
      return cells;
    }

    /// <summary>Cell that contains <paramref name="pos"/> (floor-division).</summary>
    private static (int x, int y) WorldToCell(Vector2 pos)
    {
      return ((int)Math.Floor(pos.X / GRID_SIZE),
              (int)Math.Floor(pos.Y / GRID_SIZE));
    }
  }
}
