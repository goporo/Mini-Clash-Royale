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

  private static readonly HashSet<(int x, int y)> occupiedCells = new();

  private static readonly Dictionary<int, (Vector2 pos, CardId type, EntityTeam team)> buildingById = new();


  public static void RebuildFromSnapshot(FullSnapshot snapshot)
  {
    occupiedCells.Clear();
    buildingById.Clear();
    foreach (var e in snapshot.Entities)
      if (e.IsBuilding && e.IsAlive)
        Register(e.Id, e.Position.ToUnityVector2(), e.Type, e.Team);
  }

  public static void PlaceBuilding(EntitySnapshot entity)
  {
    if (!entity.IsBuilding || !entity.IsAlive) return;
    Register(entity.Id, entity.Position.ToUnityVector2(), entity.Type, entity.Team);
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
      (CellCoord(worldPos.x), CellCoord(worldPos.y));

  public static Vector2 CellCenter(int cx, int cy) =>
      new((cx + 0.5f) * GRID_SIZE, (cy + 0.5f) * GRID_SIZE);


  // Deploy-zone state for the LOCAL player, derived from which of the enemy's Princess
  // towers are still standing. Mirrors the server's MatchManager.ComputeDeployZoneState.
  public static DeployZoneState GetLocalDeployZoneState()
  {
    EntityTeam enemy = LocalPlayerContext.IsTeam2 ? EntityTeam.Team1 : EntityTeam.Team2;

    bool negXAlive = false;
    bool posXAlive = false;
    foreach (var info in buildingById.Values)
    {
      if (info.type != CardId.PrincessTower || info.team != enemy)
        continue;
      if (BattleArena.IsNegXLane(info.pos.x))
        negXAlive = true;
      else
        posXAlive = true;
    }

    return BattleArena.GetDeployZoneState(negXAlive, posXAlive);
  }

  private static void Register(int id, Vector2 pos, CardId type, EntityTeam team)
  {
    buildingById[id] = (pos, type, team);
    foreach (var cell in GetFootprintCells(pos, type))
      occupiedCells.Add(cell);
  }

  private static IEnumerable<(int x, int y)> GetFootprintCells(Vector2 center, CardId type)
  {
    var (w, h) = BattleArena.GetBuildingSize(type);
    int x0 = Mathf.CeilToInt((center.x - w * 0.5f) / GRID_SIZE);
    int x1 = Mathf.CeilToInt((center.x + w * 0.5f) / GRID_SIZE) - 1;
    int y0 = Mathf.CeilToInt((center.y - h * 0.5f) / GRID_SIZE);
    int y1 = Mathf.CeilToInt((center.y + h * 0.5f) / GRID_SIZE) - 1;

    for (int cx = x0; cx <= x1; cx++)
      for (int cy = y0; cy <= y1; cy++)
        yield return (cx, cy);
  }

  private static int CellCoord(float worldValue)
  {
    return (int)Math.Floor(worldValue / GRID_SIZE);
  }
}
