using UnityEngine;
using Wekid.Core.Messaging;

public struct PlayCardCommand : ICommand
{
  public CardConfig CardConfig;
  public Vector3 Position;
  public EntityTeam Team;

  public PlayCardCommand(CardConfig cardConfig, Vector3 position, EntityTeam team)
  {
    CardConfig = cardConfig;
    Position = position;
    Team = team;
  }
}

