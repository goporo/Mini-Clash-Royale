using UnityEngine;

public class BoardConfig : ScriptableObject
{
  public float tileSize = 1f;
}

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



}
