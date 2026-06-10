using System;
using System.Collections.Generic;
using System.Numerics;

namespace ClashServer
{
  /// <summary>
  /// A* pathfinder operating on NavGrid cells.
  /// Returns a list of world-space waypoints (cell centres) from the cell
  /// after <paramref name="startWorld"/> to the cell of <paramref name="goalWorld"/>.
  /// Returns null when no path exists.
  /// </summary>
  public static class Pathfinder
  {
    // 8-directional neighbours.  Diagonal cost ≈ √2.
    private static readonly (int dx, int dy, float cost)[] Neighbours =
    {
      ( 1,  0, 1.000f), (-1,  0, 1.000f),
      ( 0,  1, 1.000f), ( 0, -1, 1.000f),
      ( 1,  1, 1.414f), ( 1, -1, 1.414f),
      (-1,  1, 1.414f), (-1, -1, 1.414f),
    };

    /// <summary>
    /// Find a walkable path from <paramref name="startWorld"/> to
    /// <paramref name="goalWorld"/>.
    /// If the goal cell is blocked, the nearest walkable cell is used instead.
    /// Returns null when no path exists.
    /// </summary>
    public static List<Vector2> FindPath(
      Vector2 startWorld,
      Vector2 goalWorld,
      BoardManager board)
    {
      var (sx, sy) = NavGrid.WorldToCell(startWorld);
      var (gx, gy) = NavGrid.WorldToCell(goalWorld);

      // If goal cell is blocked, find nearest walkable alternative
      if (!NavGrid.IsWalkable(gx, gy, board))
        (gx, gy) = FindNearestWalkable(gx, gy, board);

      // Already in goal cell – no movement needed
      if (sx == gx && sy == gy)
        return new List<Vector2>();

      // ── A* ───────────────────────────────────────────────────────────────
      var openSet = new MinHeap();
      var gScore = new Dictionary<(int, int), float> { [(sx, sy)] = 0f };
      var parent = new Dictionary<(int, int), (int, int)?> { [(sx, sy)] = null };
      var closed = new HashSet<(int, int)>();

      openSet.Push(Heuristic(sx, sy, gx, gy), (sx, sy));

      while (openSet.Count > 0)
      {
        var (_, cur) = openSet.Pop();

        if (cur == (gx, gy))
          return ReconstructPath(parent, cur);

        if (!closed.Add(cur)) continue;

        var (cx, cy) = cur;

        foreach (var (dx, dy, moveCost) in Neighbours)
        {
          int nx = cx + dx, ny = cy + dy;
          if (!NavGrid.IsWalkable(nx, ny, board)) continue;

          // Prevent corner-cutting: both cardinal neighbours must be walkable
          if (dx != 0 && dy != 0)
          {
            if (!NavGrid.IsWalkable(cx + dx, cy, board)) continue;
            if (!NavGrid.IsWalkable(cx, cy + dy, board)) continue;
          }

          float tentativeG = gScore[cur] + moveCost;
          if (!gScore.TryGetValue((nx, ny), out float existing) || tentativeG < existing)
          {
            gScore[(nx, ny)] = tentativeG;
            parent[(nx, ny)] = cur;
            openSet.Push(tentativeG + Heuristic(nx, ny, gx, gy), (nx, ny));
          }
        }
      }

      return null;
    }


    private static float Heuristic(int cx, int cy, int gx, int gy) =>
      MathF.Sqrt((cx - gx) * (cx - gx) + (cy - gy) * (cy - gy));

    private static List<Vector2> ReconstructPath(
      Dictionary<(int, int), (int, int)?> parent,
      (int x, int y) goal)
    {
      var result = new List<Vector2>();
      (int x, int y)? cur = goal;

      while (cur.HasValue)
      {
        result.Add(NavGrid.CellCenter(cur.Value.x, cur.Value.y));
        cur = parent[cur.Value];
      }

      result.Reverse();

      if (result.Count > 0)
        result.RemoveAt(0);

      return result;
    }

    /// <summary>
    /// Spiral outward from (cx, cy) in Manhattan rings until a walkable cell
    /// is found.  Falls back to (cx, cy) if nothing is found within radius 8.
    /// </summary>
    private static (int, int) FindNearestWalkable(int cx, int cy, BoardManager board)
    {
      if (NavGrid.IsWalkable(cx, cy, board))
        return (cx, cy);

      for (int r = 1; r <= 8; r++)
        for (int dx = -r; dx <= r; dx++)
          for (int dy = -r; dy <= r; dy++)
          {
            if (Math.Abs(dx) + Math.Abs(dy) != r) continue;
            int nx = cx + dx, ny = cy + dy;
            if (NavGrid.IsWalkable(nx, ny, board)) return (nx, ny);
          }

      return (cx, cy);
    }

    // ── Binary min-heap ───────────────────────────────────────────────────

    private sealed class MinHeap
    {
      private readonly List<(float priority, (int x, int y) cell)> _data = new();

      public int Count => _data.Count;

      public void Push(float priority, (int x, int y) cell)
      {
        _data.Add((priority, cell));
        BubbleUp(_data.Count - 1);
      }

      public (float priority, (int x, int y) cell) Pop()
      {
        var top = _data[0];
        int last = _data.Count - 1;
        _data[0] = _data[last];
        _data.RemoveAt(last);
        if (_data.Count > 0) SiftDown(0);
        return top;
      }

      private void BubbleUp(int i)
      {
        while (i > 0)
        {
          int p = (i - 1) / 2;
          if (_data[p].priority <= _data[i].priority) break;
          (_data[p], _data[i]) = (_data[i], _data[p]);
          i = p;
        }
      }

      private void SiftDown(int i)
      {
        int n = _data.Count;
        while (true)
        {
          int l = 2 * i + 1, r = 2 * i + 2, s = i;
          if (l < n && _data[l].priority < _data[s].priority) s = l;
          if (r < n && _data[r].priority < _data[s].priority) s = r;
          if (s == i) break;
          (_data[i], _data[s]) = (_data[s], _data[i]);
          i = s;
        }
      }
    }
  }
}
