using System;
using System.Collections.Generic;
using ClashShared;
using UnityEngine;

/// <summary>
/// Client-side mirror of the server's building occupancy.
/// Populated from full/delta snapshots so the drag preview can
/// snap away from occupied tiles without a server round-trip.
/// </summary>
public static class ClientBoardState
{
  public const float GRID_SIZE = 1f;

  private static readonly Dictionary<CardId, (float w, float h)> buildingSizes = new()
    {
      { CardId.PrincessTower, (3f, 3f) },
      { CardId.KingTower, (4f, 4f) },
      { CardId.Block, (1f, 1f) },
    };

  private static readonly HashSet<(int x, int y)> occupiedCells = new();

  private static readonly Dictionary<int, (Vector2 pos, CardId type)> buildingById = new();


  public static void RebuildFromSnapshot(FullSnapshot snapshot)
  {
    occupiedCells.Clear();
    buildingById.Clear();
    foreach (var e in snapshot.Entities)
      if (e.IsBuilding && e.IsAlive)
        Register(e.Id, e.Position.ToUnityVector2(), e.Type);
  }

  public static void PlaceBuilding(EntitySnapshot entity)
  {
    if (!entity.IsBuilding || !entity.IsAlive) return;
    Register(entity.Id, entity.Position.ToUnityVector2(), entity.Type);
  }

  public static void RemoveBuildingById(int entityId)
  {
    if (!buildingById.TryGetValue(entityId, out var info)) return;
    buildingById.Remove(entityId);
    foreach (var cell in GetFootprintCells(info.pos, info.type))
      occupiedCells.Remove(cell);
  }

  public static bool IsCellOccupied(int cx, int cy) =>
      occupiedCells.Contains((cx, cy));

  public static (int x, int y) WorldToCell(Vector2 worldPos) =>
      ((int)Math.Floor(worldPos.x / GRID_SIZE),
       (int)Math.Floor(worldPos.y / GRID_SIZE));

  public static Vector2 CellCenter(int cx, int cy) =>
      new((cx + 0.5f) * GRID_SIZE, (cy + 0.5f) * GRID_SIZE);


  private static void Register(int id, Vector2 pos, CardId type)
  {
    buildingById[id] = (pos, type);
    foreach (var cell in GetFootprintCells(pos, type))
      occupiedCells.Add(cell);
  }

  /// <summary>
  /// All cells whose area [C*G, (C+1)*G) overlaps the building footprint.
  /// Uses the same formula as BoardManager.GetFootprintCells.
  /// </summary>
  private static IEnumerable<(int x, int y)> GetFootprintCells(Vector2 center, CardId type)
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
