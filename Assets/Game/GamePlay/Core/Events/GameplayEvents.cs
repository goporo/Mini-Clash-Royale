using Wekid.Core.Messaging;
using UnityEngine;

[DontLogEvent]
public struct SimpleDontLogEventEvent : IGameEvent
{
  public Entity Entity;
  public SimpleDontLogEventEvent(Entity Entity)
  {
    this.Entity = Entity;
  }
}


public struct CardPlayedEvent : IGameEvent
{
  public CardConfig CardConfig;
  public Vector3 Position;
  public EntityTeam Team;

  public CardPlayedEvent(CardConfig cardConfig, Vector3 position, EntityTeam team)
  {
    CardConfig = cardConfig;
    Position = position;
    Team = team;
  }
}

