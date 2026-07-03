using ClashShared;
using UnityEngine;
using UnityEngine.Serialization;

public enum SpawnOverlayState
{
    None,
    Full,
    LeftOnly,
    RightOnly
}

// Overlay sits over the ENEMY half and marks where you CANNOT deploy.
// All blockers are on at the start of a drag; destroying an enemy Princess tower
// REMOVES the blocker on that tower's lane, exposing the newly unlocked deploy area.
public class SpawnOverlayView : MonoBehaviour
{
    [Tooltip("Always-on overlay piece (e.g. strip near the river). Not tied to tower state.")]
    [FormerlySerializedAs("full")]
    [SerializeField] private GameObject lower;

    [Tooltip("Blocker over the enemy LEFT lane (player's view). Hidden once that lane is unlocked.")]
    [FormerlySerializedAs("left")]
    [SerializeField] private GameObject leftHalf;

    [Tooltip("Blocker over the enemy RIGHT lane (player's view). Hidden once that lane is unlocked.")]
    [FormerlySerializedAs("right")]
    [SerializeField] private GameObject rightHalf;

    // Show the enemy-half blockers, removing the lane(s) unlocked by destroying an enemy
    // Princess tower. A removed blocker = that lane is now deployable.
    public void Show(DeployZoneState zone)
    {
        // Zone flags are in absolute board X. For Team2 the board is visually X-mirrored,
        // so the enemy's negative-X tower appears on the player's RIGHT — flip accordingly.
        bool leftLaneUnlocked = LocalPlayerContext.IsTeam2 ? zone.EnemyPosXTowerDown : zone.EnemyNegXTowerDown;
        bool rightLaneUnlocked = LocalPlayerContext.IsTeam2 ? zone.EnemyNegXTowerDown : zone.EnemyPosXTowerDown;

        if (lower != null) lower.SetActive(true);
        if (leftHalf != null) leftHalf.SetActive(!leftLaneUnlocked);
        if (rightHalf != null) rightHalf.SetActive(!rightLaneUnlocked);
    }

    public void Hide()
    {
        if (lower != null) lower.SetActive(false);
        if (leftHalf != null) leftHalf.SetActive(false);
        if (rightHalf != null) rightHalf.SetActive(false);
    }

    // Legacy single-state API kept for any existing callers.
    public void SetState(SpawnOverlayState state)
    {
        switch (state)
        {
            case SpawnOverlayState.None:
                Hide();
                break;
            case SpawnOverlayState.Full:
                Show(DeployZoneState.None);
                break;
            case SpawnOverlayState.LeftOnly:
                Show(new DeployZoneState(enemyNegXTowerDown: false, enemyPosXTowerDown: true));
                break;
            case SpawnOverlayState.RightOnly:
                Show(new DeployZoneState(enemyNegXTowerDown: true, enemyPosXTowerDown: false));
                break;
        }
    }
}
