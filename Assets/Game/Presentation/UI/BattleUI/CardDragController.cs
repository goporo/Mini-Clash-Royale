using UnityEngine;
using UnityEngine.EventSystems;

public class CardDragController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
  public CardSlotUI slot;
  public CardPlacementPreview preview;

  private Camera cam;

  void Start()
  {
    cam = Camera.main;
  }

  public void OnBeginDrag(PointerEventData eventData)
  {
    preview.Show(slot.card);
  }

  public void OnDrag(PointerEventData eventData)
  {
    Vector3 worldPos = GetWorldPos(eventData.position);
    preview.UpdatePosition(worldPos);
  }

  public void OnEndDrag(PointerEventData eventData)
  {
    Vector3 worldPos = GetWorldPos(eventData.position);
    Vector3 spawnPos = new Vector3(worldPos.x, 0.5f, worldPos.z);

    if (preview.CanSpawnAt(spawnPos))
    {
      preview.Spawn(spawnPos);
    }

    preview.Hide();
  }

  private Vector3 GetWorldPos(Vector2 screenPos)
  {
    Ray ray = cam.ScreenPointToRay(screenPos);
    if (Physics.Raycast(ray, out RaycastHit hit))
      return hit.point;

    return Vector3.zero;
  }
}
