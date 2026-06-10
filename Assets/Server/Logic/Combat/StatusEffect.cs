using ClashShared;

namespace ClashServer
{
  public sealed class ActiveStatusEffect
  {
    public StatusKind Kind;
    public int RemainingTicks;
    public float Magnitude;
  }

  public readonly struct PendingStatus
  {
    public readonly ServerEntity Target;
    public readonly StatusKind Kind;
    public readonly int DurationTicks;
    public readonly float Magnitude;

    public PendingStatus(ServerEntity target, StatusKind kind, int durationTicks, float magnitude)
    {
      Target = target;
      Kind = kind;
      DurationTicks = durationTicks;
      Magnitude = magnitude;
    }
  }
}
