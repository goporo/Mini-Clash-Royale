namespace ClashShared
{
  public enum CardId : ushort
  {
    None = 0,

    // Troop cards (playable from hand)
    Knight = 100,
    Archer = 101,
    Giant = 102,
    Cannon = 103,
    Goblin = 104,
    Fireball = 105,
    Musketeer = 106,
    MiniPekka = 107,

    // Building entity types (spawned on match init, not from hand)
    Block = 200,
    PrincessTower = 201,
    KingTower = 202,
  }

  public enum SpawnFormation
  {
    Single = 0,        // exact position
    DuoLine = 1,       // two units side by side (+-x)
    TrioTriangle = 2,  // three units in a triangle
    QuadDiamond = 3,   // four units in a diamond
    QuadLine = 4,      // four units in a horizontal line
    Square = 5,        // four units in a square
  }

  public enum CardType
  {
    Troop = 0,
    Building = 1,
    Spell = 2,
  }

  public enum PlacementRule
  {
    OwnSideOnly = 0,
    Anywhere = 1,
    EnemySideOnly = 2,
    OwnTerritoryIncludingRiver = 3
  }

  public enum EntityTeam
  {
    Team1 = 0,
    Team2 = 1
  }

  public static class CardCostTable
  {
    public static int GetMilliElixirCost(CardId cardId)
    {
      switch (cardId)
      {
        case CardId.Knight: return 3000;
        case CardId.Archer: return 3000;
        case CardId.Giant: return 5000;
        case CardId.Cannon: return 3000;
        case CardId.Goblin: return 2000;
        case CardId.Fireball: return 4000;
        case CardId.Musketeer: return 4000;
        case CardId.MiniPekka: return 4000;
        default: return 3000;
      }
    }
  }
}
