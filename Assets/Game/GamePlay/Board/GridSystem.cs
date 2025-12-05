using UnityEngine;

public class GridSystem
{
  private readonly float cellSize;

  public GridSystem(float cellSize)
  {
    this.cellSize = cellSize;
  }

  public Vector3 CellToWorld(Vector2Int cell)
  {
    return new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);
  }

  public Vector2Int WorldToCell(Vector3 pos)
  {
    return new Vector2Int(
        Mathf.FloorToInt(pos.x / cellSize),
        Mathf.FloorToInt(pos.z / cellSize)
    );
  }
}
