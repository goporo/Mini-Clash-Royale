using UnityEngine;

public enum SpawnOverlayState
{
    None,
    Full,
    LeftOnly,
    RightOnly
}

public class SpawnOverlayView : MonoBehaviour
{
    [SerializeField] private GameObject full;
    [SerializeField] private GameObject left;
    [SerializeField] private GameObject right;

    public void SetState(SpawnOverlayState state)
    {
        full.SetActive(false);
        left.SetActive(false);
        right.SetActive(false);

        switch (state)
        {
            case SpawnOverlayState.Full:
                full.SetActive(true);
                break;

            case SpawnOverlayState.LeftOnly:
                left.SetActive(true);
                break;

            case SpawnOverlayState.RightOnly:
                right.SetActive(true);
                break;

            case SpawnOverlayState.None:
                break;
        }
    }
}
