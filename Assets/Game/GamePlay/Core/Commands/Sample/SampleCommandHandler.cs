using Wekid.Core.Messaging;
using UnityEngine;

[CommandHandler]
public class SampleCommandHandler : ICommandHandler<SampleCommand>
{
  public void Execute(SampleCommand cmd)
  {
    // Next step → Validate placement → Fire Event → Spawn
    GameplayEvents.Publish(new CardPlayedEvent(cmd.Position));
  }
}