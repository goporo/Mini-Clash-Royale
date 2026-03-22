using System;
using Wekid.Core.Messaging;

public static class GameplayEvents
{
  private static readonly EventBus _bus = new();

  public static void Subscribe<T>(Action<T> handler) where T : IGameEvent
      => _bus.Subscribe(handler);

  public static void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
      => _bus.Unsubscribe(handler);

  public static void Publish<T>(T evt) where T : IGameEvent
      => _bus.Publish(evt);

  public static void Clear() => _bus.Clear();
}