using UnityEngine;
using Wekid.Core.Messaging;

public struct SampleCommand : ICommand
{
  public Vector3 Position;

  public SampleCommand(Vector3 position)
  {
    Position = position;
  }
}

