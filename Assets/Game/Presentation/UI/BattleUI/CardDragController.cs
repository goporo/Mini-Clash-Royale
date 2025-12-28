using UnityEngine;
using UnityEngine.EventSystems;

public class CardDragController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
  public CardSlotUI slot;
  public CardPlacementPreview preview;
  private PlayerNetwork playerNetwork;

  private Camera cam;

  void Start()
  {
    cam = Camera.main;

  }

  public void OnBeginDrag(PointerEventData eventData)
  {
    preview.Show(slot.Config);
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

    playerNetwork = PlayerNetwork.LocalPlayer;
    if (playerNetwork == null)
    {
      Debug.LogError("PlayerNetwork not found in scene!");
      preview.Hide();
      return;
    }

    playerNetwork.PlayCard(1, "knight", new Vector2(spawnPos.x, spawnPos.z));

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
