using Wekid.Core.Messaging;
using UnityEngine;

[CommandHandler]
public class PlayCardCommandHandler : ICommandHandler<PlayCardCommand>
{
  public void Execute(PlayCardCommand cmd)
  {
    Debug.Log($"PlayCardCommand → {cmd.CardConfig} at {cmd.Position}");
    // Next step → Validate placement → Fire Event → Spawn
    GameplayEvents.Publish(new CardPlayedEvent(cmd.CardConfig, cmd.Position, cmd.Team));
  }
}