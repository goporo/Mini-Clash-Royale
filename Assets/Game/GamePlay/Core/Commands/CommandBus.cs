using Wekid.Core.Messaging;

public static class GameplayCommandBus
{
  public static readonly CommandBus Instance = new();
}