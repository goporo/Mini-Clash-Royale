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
    Fireball = 104,

    // Building entity types (spawned on match init, not from hand)
    Block = 200,
    PrincessTower = 201,
    KingTower = 202,
  }
}