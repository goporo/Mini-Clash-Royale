namespace ClashShared
{
  public readonly struct SpawnUnit
  {
    public readonly CardId EntityType;
    public readonly SpawnFormation Formation;
    public readonly float OffsetX;
    public readonly float OffsetY;

    public SpawnUnit(CardId type, SpawnFormation formation, float offsetX = 0f, float offsetY = 0f)
    {
      EntityType = type;
      Formation = formation;
      OffsetX = offsetX;
      OffsetY = offsetY;
    }
  }

  // Which of the *enemy's* Princess towers have fallen, in absolute board coords.
  // Destroying an enemy Princess tower unlocks deployment forward into the enemy half
  // on that tower's side, up to the tower line (Clash Royale rules).
  public readonly struct DeployZoneState
  {
    // "Negative-X side" and "positive-X side" refer to absolute board X, not the
    // attacker's perspective — keeps the math symmetric for both teams.
    public readonly bool EnemyNegXTowerDown;
    public readonly bool EnemyPosXTowerDown;

    public DeployZoneState(bool enemyNegXTowerDown, bool enemyPosXTowerDown)
    {
      EnemyNegXTowerDown = enemyNegXTowerDown;
      EnemyPosXTowerDown = enemyPosXTowerDown;
    }

    public static DeployZoneState None => new DeployZoneState(false, false);
  }

  public static class BattleArena
  {
    public const float GridSize = 0.5f;
    public const float Left = -9f;
    public const float Right = 9f;
    public const float Bottom = -16f;
    public const float Top = 16f;
    public const float RiverBottom = -1f;
    public const float RiverTop = 1f;

    // Y of the forward deploy line when an enemy Princess tower is destroyed.
    // Extends 4 tiles past the river edge (RiverTop = 1, so 1 + 4 = 5).
    public const float PrincessTowerLine = 5.0f;
    // X coordinate of the Princess towers; also the divider between left/right lanes.
    public const float PrincessTowerX = 5.5f;

    public static bool IsInsideArena(float x, float y)
    {
      return x >= Left && x <= Right && y >= Bottom && y <= Top;
    }

    // Build the deploy-zone state for `team` given whether each of the ENEMY's two Princess
    // towers (by absolute X side) is still alive. A side is "unlocked" once its tower is gone.
    public static DeployZoneState GetDeployZoneState(bool enemyNegXTowerAlive, bool enemyPosXTowerAlive)
    {
      return new DeployZoneState(!enemyNegXTowerAlive, !enemyPosXTowerAlive);
    }

    // True if the given Princess tower X sits on the negative-X lane.
    public static bool IsNegXLane(float towerX) => towerX < 0f;

    public static bool IsInsideDeployZone(EntityTeam team, float x, float y)
    {
      return IsInsideDeployZone(team, x, y, DeployZoneState.None);
    }

    public static bool IsInsideDeployZone(EntityTeam team, float x, float y, DeployZoneState zone)
    {
      if (x < Left || x > Right)
        return false;

      // Base half-board zone (own side up to the river).
      bool inBase = team == EntityTeam.Team1
        ? y >= Bottom && y <= RiverBottom
        : y >= RiverTop && y <= Top;
      if (inBase)
        return true;

      // Forward extension into the enemy half when an enemy Princess tower is down.
      bool sideUnlocked = x < 0f ? zone.EnemyNegXTowerDown : zone.EnemyPosXTowerDown;
      if (!sideUnlocked)
        return false;

      return team == EntityTeam.Team1
        ? y >= RiverTop && y <= PrincessTowerLine
        : y <= RiverBottom && y >= -PrincessTowerLine;
    }

    // Like IsInsideDeployZone but also guarantees the building footprint (w×h) does not
    // overlap the river. The center must be far enough from the river edge that the
    // full footprint clears it.
    public static bool IsInsideBuildingDeployZone(EntityTeam team, float cx, float cy, float halfW, float halfH, DeployZoneState zone)
    {
      // Footprint bounds
      float left  = cx - halfW;
      float right = cx + halfW;
      float bot   = cy - halfH;
      float top   = cy + halfH;

      // Must be within arena X
      if (left < Left || right > Right)
        return false;

      // Footprint must not touch the river band at all
      bool crossesRiver = bot < RiverTop && top > RiverBottom;
      if (crossesRiver)
        return false;

      // Check center against zone (the zone math already handles the base/extended split)
      return IsInsideDeployZone(team, cx, cy, zone);
    }

    public static bool TryGetCardType(CardId cardId, out CardType cardType)
    {
      switch (cardId)
      {
        case CardId.Knight:
        case CardId.Archer:
        case CardId.Giant:
        case CardId.Goblin:
        case CardId.Musketeer:
        case CardId.MiniPekka:
        case CardId.Valkyrie:
        case CardId.Bomber:
        case CardId.IceWizard:
        case CardId.IceGolem:
        case CardId.WallBreakers:
        case CardId.Skeletons:
        case CardId.SpearGoblins:
        case CardId.GoblinGang:
        case CardId.Minions:
        case CardId.SkeletonBarrel:
          cardType = CardType.Troop;
          return true;
        case CardId.Cannon:
        case CardId.Block:
        case CardId.PrincessTower:
        case CardId.KingTower:
          cardType = CardType.Building;
          return true;
        case CardId.Fireball:
          cardType = CardType.Spell;
          return true;
        default:
          cardType = default;
          return false;
      }
    }

    public static SpawnFormation GetSpawnFormation(CardId cardId) => cardId switch
    {
      CardId.Archer => SpawnFormation.DuoLine,
      CardId.Goblin => SpawnFormation.Square,
      CardId.WallBreakers => SpawnFormation.DuoLine,
      CardId.Skeletons => SpawnFormation.TrioTriangle,
      CardId.SpearGoblins => SpawnFormation.TrioTriangle,
      CardId.Minions => SpawnFormation.TrioTriangle,
      _ => SpawnFormation.Single
    };

    public static SpawnUnit[] GetSpawnRecipe(CardId cardId, EntityTeam team)
    {
      float dir = team == EntityTeam.Team1 ? 1f : -1f;
      return cardId switch
      {
        CardId.GoblinGang => new[]
        {
          new SpawnUnit(CardId.Goblin,       SpawnFormation.TrioTriangle, 0f,  dir * 0.5f),
          new SpawnUnit(CardId.SpearGoblins, SpawnFormation.DuoLine,      0f, -dir * 0.5f)
        },
        _ => new[] { new SpawnUnit(cardId, GetSpawnFormation(cardId)) }
      };
    }

    public static (float width, float height) GetBuildingSize(CardId cardId) => cardId switch
    {
      CardId.PrincessTower => (3f, 3f),
      CardId.KingTower => (4f, 4f),
      CardId.Cannon => (3f, 3f),
      CardId.Block => (1f, 1f),
      _ => (1f, 1f)
    };
  }
}
