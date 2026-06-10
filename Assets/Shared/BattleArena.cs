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

  public static class BattleArena
  {
    public const float GridSize = 0.5f;
    public const float Left = -9f;
    public const float Right = 9f;
    public const float Bottom = -16f;
    public const float Top = 16f;
    public const float RiverBottom = -1f;
    public const float RiverTop = 1f;

    public static bool IsInsideArena(float x, float y)
    {
      return x >= Left && x <= Right && y >= Bottom && y <= Top;
    }

    public static bool IsInsideDeployZone(EntityTeam team, float x, float y)
    {
      if (x < Left || x > Right)
        return false;

      return team == EntityTeam.Team1
        ? y >= Bottom && y <= RiverBottom
        : y >= RiverTop && y <= Top;
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
