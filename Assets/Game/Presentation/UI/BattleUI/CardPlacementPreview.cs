using UnityEngine;

public class CardPlacementPreview : MonoBehaviour
{
  public GameObject ghostPrefab;
  private GameObject ghost;

  public void Show(CardConfig card)
  {
    ghost = Instantiate(ghostPrefab);
  }

  public void UpdatePosition(Vector3 pos)
  {
    if (ghost == null) return;

    ghost.transform.position = pos;

    bool valid = true;
    ghost.GetComponent<Renderer>().material.color = valid ? Color.green : Color.red;
  }

  public void Hide()
  {
    if (ghost != null) Destroy(ghost);
    ghost = null;
  }
}
