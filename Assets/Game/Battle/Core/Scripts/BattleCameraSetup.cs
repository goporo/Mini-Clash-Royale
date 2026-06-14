using UnityEngine;

// No camera transform changes are needed.
// Team2's view is handled by mirroring entity positions in LocalPlayerContext.ToVisual().
// This keeps the camera, Canvas UI, and raycasts identical for both players.
public static class BattleCameraSetup
{
  public static void Apply()
  {
    // intentionally empty — visual flip is done per-entity via LocalPlayerContext.ToVisual()
  }
}
