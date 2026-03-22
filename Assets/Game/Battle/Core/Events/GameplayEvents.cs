using Wekid.Core.Messaging;
using UnityEngine;

[DontLogEvent]
public struct SimpleDontLogEventEvent : IGameEvent
{
}


public struct CardPlayedEvent : IGameEvent
{
  public Vector3 Position;

  public CardPlayedEvent(Vector3 position)
  {
    Position = position;
  }
}

