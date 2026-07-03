using UnityEngine;
using ClashShared;

public static class ClientCardPlacementService
{
  private const float GRID_SIZE = ClientBoardState.GRID_SIZE;

  // rawWorldPos — direct raycast hit from the camera (never moves, so this is
  //               always server world-space for Team1 and mirrored for Team2).
  // snappedWorldPos — server world-space position to send to the server.
  public static bool TryGetPlacement(Vector3 rawWorldPos, PlacementRule rule, out Vector2 snappedWorldPos)
  {
    return TryGetPlacement(rawWorldPos, rule, CardId.Knight, out snappedWorldPos);
  }

  public static bool TryGetPlacement(Vector3 rawWorldPos, PlacementRule rule, CardId cardId, out Vector2 snappedWorldPos)
  {
    if (rawWorldPos == Vector3.zero)
    {
      snappedWorldPos = default;
      return false;
    }

    // Mirror the hit point to the local player's half of the world.
    // For Team1: identity.  For Team2: negate X and Z.
    Vector2 worldXZ = LocalPlayerContext.ToWorld(new Vector2(rawWorldPos.x, rawWorldPos.z));

    // Deploy zone in server world-space for the local player, including any forward
    // extension unlocked by destroying an enemy Princess tower.
    EntityTeam team = LocalPlayerContext.LocalTeam;
    DeployZoneState zone = ClientBoardState.GetLocalDeployZoneState();

    bool anywhere = rule == PlacementRule.Anywhere;
    bool forwardUnlocked = zone.EnemyNegXTowerDown || zone.EnemyPosXTowerDown;

    // For buildings, footprint half-height shrinks the valid Y range to prevent river overlap.
    BattleArena.TryGetCardType(cardId, out CardType cardType);
    bool isBuilding = cardType == CardType.Building;
    float halfH = isBuilding ? BattleArena.GetBuildingSize(cardId).height * 0.5f : 0f;

    // Y clamp bounds: base half + forward reach to the tower line when a lane is unlocked.
    float minY, maxY;
    if (anywhere)
    {
      minY = BattleArena.Bottom;
      maxY = BattleArena.Top;
    }
    else if (LocalPlayerContext.IsTeam2)
    {
      minY = forwardUnlocked ? -BattleArena.PrincessTowerLine : BattleArena.RiverTop;
      maxY = BattleArena.Top;
    }
    else
    {
      minY = BattleArena.Bottom;
      maxY = forwardUnlocked ? BattleArena.PrincessTowerLine : BattleArena.RiverBottom;
    }

    snappedWorldPos = SnapToCell(worldXZ, minY, maxY, rule, cardId, isBuilding, team, zone);
    return true;
  }

  private static Vector2 SnapToCell(Vector2 worldXZ, float minY, float maxY, PlacementRule rule, CardId cardId, bool isBuilding, EntityTeam team, DeployZoneState zone)
  {
    float wx = Mathf.Clamp(worldXZ.x, BattleArena.Left, BattleArena.Right);
    float wy = Mathf.Clamp(worldXZ.y, minY, maxY);

    int minCX = Mathf.FloorToInt((BattleArena.Left  + GRID_SIZE * 0.5f) / GRID_SIZE);
    int maxCX = Mathf.FloorToInt((BattleArena.Right - GRID_SIZE * 0.5f) / GRID_SIZE);
    int minCY = Mathf.FloorToInt((minY + GRID_SIZE * 0.5f) / GRID_SIZE);
    int maxCY = Mathf.FloorToInt((maxY - GRID_SIZE * 0.5f) / GRID_SIZE);

    var (cx, cy) = ClientBoardState.WorldToCell(new Vector2(wx, wy));
    cx = Mathf.Clamp(cx, minCX, maxCX);
    cy = Mathf.Clamp(cy, minCY, maxCY);

    var (freeCX, freeCY) = FindNearestFreeCell(cx, cy, minCX, maxCX, minCY, maxCY, rule, cardId, isBuilding, team, zone);
    return ClientBoardState.CellCenter(freeCX, freeCY);
  }

  // A cell is valid only if its center lies in the actual deploy zone, and for buildings
  // the entire footprint must clear the river. The Y-clamp rectangle is a superset (it
  // spans the forward reach across the full width), so this per-cell check is what
  // enforces the per-lane unlock: a forward cell on a still-locked side is rejected.
  private static bool IsCellInZone(int cx, int cy, PlacementRule rule, CardId cardId, bool isBuilding, EntityTeam team, DeployZoneState zone)
  {
    if (rule == PlacementRule.Anywhere)
      return true;

    Vector2 center = ClientBoardState.CellCenter(cx, cy);

    if (isBuilding)
    {
      var (w, h) = BattleArena.GetBuildingSize(cardId);
      return BattleArena.IsInsideBuildingDeployZone(team, center.x, center.y, w * 0.5f, h * 0.5f, zone);
    }

    return BattleArena.IsInsideDeployZone(team, center.x, center.y, zone);
  }

  private static (int x, int y) FindNearestFreeCell(
      int cx, int cy, int minCX, int maxCX, int minCY, int maxCY, PlacementRule rule, CardId cardId, bool isBuilding, EntityTeam team, DeployZoneState zone)
  {
    if (rule == PlacementRule.Anywhere)
      return (cx, cy);

    if (!ClientBoardState.IsCellOccupied(cx, cy) && IsCellInZone(cx, cy, rule, cardId, isBuilding, team, zone))
      return (cx, cy);

    for (int r = 1; r <= 12; r++)
    {
      (int x, int y)? best = null;
      float bestDistSq = float.MaxValue;

      for (int dx = -r; dx <= r; dx++)
      {
        for (int dy = -r; dy <= r; dy++)
        {
          if (Mathf.Abs(dx) + Mathf.Abs(dy) != r)
            continue;

          int nx = cx + dx;
          int ny = cy + dy;
          if (nx < minCX || nx > maxCX || ny < minCY || ny > maxCY)
            continue;
          if (ClientBoardState.IsCellOccupied(nx, ny))
            continue;
          if (!IsCellInZone(nx, ny, rule, cardId, isBuilding, team, zone))
            continue;

          float distanceSq = dx * dx + dy * dy;
          if (distanceSq < bestDistSq)
          {
            bestDistSq = distanceSq;
            best = (nx, ny);
          }
        }
      }

      if (best.HasValue)
        return best.Value;
    }

    return (cx, cy);
  }
}
