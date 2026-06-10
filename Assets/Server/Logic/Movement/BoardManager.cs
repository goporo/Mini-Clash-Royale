using System;
using System.Collections.Generic;
using System.Numerics;
using ClashShared;

namespace ClashServer
{
  public class BoardManager
  {
    public const float GRID_SIZE = BattleArena.GridSize;

    private readonly Dictionary<(int x, int y), ServerEntity> buildingGrid = new();

    private readonly Dictionary<int, List<(int x, int y)>> entityToCells = new();

    public void PlaceBuilding(ServerEntity entity)
    {
      var cells = GetFootprintCells(entity.Position, entity.Type);
      entityToCells[entity.Id] = cells;
      foreach (var cell in cells)
        buildingGrid[cell] = entity;
    }

    public void RemoveBuilding(int entityId)
    {
      if (!entityToCells.TryGetValue(entityId, out var cells))
        return;

      foreach (var cell in cells)
        buildingGrid.Remove(cell);
      entityToCells.Remove(entityId);
    }

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

    public IReadOnlyList<ServerEntity> GetAllBuildings()
    {
      var live = new List<ServerEntity>();
      var seen = new HashSet<int>();
      foreach (var entity in buildingGrid.Values)
        if (entity.IsAlive && seen.Add(entity.Id))
          live.Add(entity);
      return live;
    }

    public void Clear()
    {
      buildingGrid.Clear();
      entityToCells.Clear();
    }
    private static List<(int x, int y)> GetFootprintCells(Vector2 center, CardId type)
    {
      var (w, h) = BattleArena.GetBuildingSize(type);
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

    private static (int x, int y) WorldToCell(Vector2 pos)
    {
      return (CellCoord(pos.X),
              CellCoord(pos.Y));
    }

    private static int CellCoord(float worldValue)
    {
      return (int)Math.Floor(worldValue / GRID_SIZE);
    }
  }
}
