using UnityEngine;

public class Board
{
  public BoardConfig Config { get; private set; }
  public GridSystem Grid { get; private set; }

  public Board(BoardConfig config)
  {
    Config = config;
    Grid = new GridSystem(config.tileSize);
  }

  public Vector3 GetWorldPosition(Vector2Int cell)
      => Grid.CellToWorld(cell);

  public Vector2Int GetCell(Vector3 worldPos)
      => Grid.WorldToCell(worldPos);

  public bool IsInsideLane(Vector3 worldPos, int laneIndex)
  {
    var lane = Config.lanes[laneIndex];
    return worldPos.x >= lane.xMin && worldPos.x <= lane.xMax;
  }

  public bool IsValidSpawnPosition(Vector3 worldPos, CardSpawnRules rules, EntityTeam team)
  {
    // EXAMPLE RULE: restrict to own half
    if (rules.restrictToOwnSide)
    {
      if (team == EntityTeam.Team1 && worldPos.z > Config.boardHeight * 0.5f)
        return false;
      if (team == EntityTeam.Team2 && worldPos.z < Config.boardHeight * 0.5f)
        return false;
    }

    // EXAMPLE RULE: restrict to lanes
    if (rules.restrictToLanes)
    {
      bool inLane = false;
      foreach (var lane in Config.lanes)
      {
        if (worldPos.x >= lane.xMin && worldPos.x <= lane.xMax)
        {
          inLane = true;
          break;
        }
      }
      if (!inLane) return false;
    }

    return true;
  }

}
