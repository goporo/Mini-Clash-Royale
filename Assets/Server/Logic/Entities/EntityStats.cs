namespace ClashServer
{
  public struct EntityStats
  {
    // CardStats.MoveSpeed uses Clash Royale's internal unit (tiles/min × 2).
    // Divide by 60 to get tiles/sec before any per-tick math.
    private const float CardSpeedToTilesPerSec = 1f / 40f;

    public float MaxHP;
    public float CurrentHP;
    public float MoveSpeed;      // tiles/sec
    public float MovePerTick;    // tiles/tick

    public EntityStats(float maxHP, float cardMoveSpeed)
    {
      MaxHP = maxHP;
      CurrentHP = maxHP;
      MoveSpeed = cardMoveSpeed * CardSpeedToTilesPerSec;
      MovePerTick = MoveSpeed * ServerTickSettings.FixedDeltaTime;
    }
  }
}
