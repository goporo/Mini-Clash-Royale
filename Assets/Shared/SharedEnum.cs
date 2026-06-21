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
    Valkyrie = 108,
    Bomber = 109,
    IceWizard = 110,
    IceGolem = 111,
    WallBreakers = 112,
    SkeletonBarrel = 113,
    GoblinGang = 114,
    Minions = 115,
    Skeletons = 116,
    SpearGoblins = 117,

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
  }

  public enum EntityTeam
  {
    Team1 = 0,
    Team2 = 1
  }

  public enum StatusKind { Slow }

  public static class CardIdHelper
  {
    public static CardId[] GetAllPlayableCards() =>
      System.Array.FindAll(
        (CardId[])System.Enum.GetValues(typeof(CardId)),
        id => (ushort)id is >= 100 and <= 199);
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
        case CardId.Valkyrie: return 4000;
        case CardId.Bomber: return 3000;
        case CardId.IceWizard: return 3000;
        case CardId.IceGolem: return 2000;
        case CardId.WallBreakers: return 2000;
        case CardId.Skeletons: return 1000;
        case CardId.SpearGoblins: return 2000;
        case CardId.GoblinGang: return 3000;
        case CardId.Minions: return 3000;
        case CardId.SkeletonBarrel: return 3000;
        default: return 3000;
      }
    }
  }
}
