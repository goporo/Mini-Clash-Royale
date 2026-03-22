namespace ClashServer
{
  public class ElixirState
  {
    public int Current;  // milli-elixir
    public int Max;

    // 1 elixir per 2.8 s at 0.1 s/tick -> 1000 / (2.8 / 0.1) = 1000 / 28 ~= 35.714 milli-elixir per tick
    private const float BASE_REGEN_PER_TICK = 1000f / 28f * 3;
    private float regenRate;
    private float regenAccumulator;

    public ElixirState()
    {
      Max = 10000;        // 10 elixir
      Current = 5000;     // start at 5 elixir
      regenRate = BASE_REGEN_PER_TICK;
    }

    public void TickRegen()
    {
      regenAccumulator += regenRate;
      int toAdd = (int)regenAccumulator;
      if (toAdd > 0)
      {
        regenAccumulator -= toAdd;
        int newCurrent = Current + toAdd;
        Current = newCurrent > Max ? Max : newCurrent;
      }
    }

    public bool CanSpend(int amount)
    {
      return Current >= amount;
    }

    public bool TrySpend(int amount)
    {
      if (Current >= amount)
      {
        Current -= amount;
        return true;
      }

      return false;
    }

    public void SetRegenRate(float multiplier)
    {
      regenRate = BASE_REGEN_PER_TICK * multiplier;
    }
  }
}
