using UnityEngine;
using UnityEngine.EventSystems;
using ClashShared;

public class CardDragController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
  public CardSlotUI slot;
  public CardPlacementPreview preview;
  public SpawnOverlayView spawnOverlay;

  [Tooltip("Index of this slot in BattleHand.handSlots (0-3), for local UI cycling.")]
  public int slotIndex;

  private Camera cam;
  private Vector3 _cardInitialScreenPos;
  private bool _previewActive;

  void Start()
  {
    cam = Camera.main;

  }

  public void OnBeginDrag(PointerEventData eventData)
  {
    _cardInitialScreenPos = slot.transform.position;
    _previewActive = false;
    var state = GetCurrentOverlayState();
    spawnOverlay.SetState(state);
  }

  private SpawnOverlayState GetCurrentOverlayState()
  {
    if (slot.Config.CardType == CardType.Spell)
      return SpawnOverlayState.None;
    return SpawnOverlayState.Full;
  }

  public void OnDrag(PointerEventData eventData)
  {
    Vector3 worldPos = GetWorldPos(eventData.position);
    bool inField = ClientCardPlacementService.TryGetPlacement(worldPos, slot.Config.PlacementRule, out Vector2 snappedPosition);

    if (inField)
    {
      slot.SetScale(0f);
      if (!_previewActive)
      {
        preview.Show(slot.Config);
        _previewActive = true;
      }
      preview.UpdatePosition(new Vector3(snappedPosition.x, worldPos.y, snappedPosition.y));
    }
    else
    {
      float fieldBottomScreenY = cam.WorldToScreenPoint(new Vector3(0f, 0f, ClientCardPlacementService.BottomWorldY)).y;
      float t = Mathf.Clamp01(Mathf.InverseLerp(_cardInitialScreenPos.y, fieldBottomScreenY, eventData.position.y));
      slot.SetUIPosition(eventData.position);
      slot.SetScale(Mathf.Lerp(1f, 0f, t));
      if (_previewActive)
      {
        preview.Hide();
        _previewActive = false;
      }
    }
  }

  public void OnEndDrag(PointerEventData eventData)
  {
    spawnOverlay.SetState(SpawnOverlayState.None);
    slot.SetUIPosition(_cardInitialScreenPos);
    slot.SetScale(1f);

    Vector3 worldPos = GetWorldPos(eventData.position);

    if (!_previewActive ||
        !ClientCardPlacementService.TryGetPlacement(worldPos, slot.Config.PlacementRule, out Vector2 validated))
    {
      preview.Hide();
      _previewActive = false;
      return;
    }

    Vector3 spawnPos = new(validated.x, 0.5f, validated.y);

    if (!ClientCardPlayService.TryPlayCard(slot.Config, new Vector2(spawnPos.x, spawnPos.z), slotIndex))
    {
      preview.Hide();
      return;
    }

    preview.Hide();
    _previewActive = false;
  }

  private Vector3 GetWorldPos(Vector2 screenPos)
  {
    Ray ray = cam.ScreenPointToRay(screenPos);
    if (Physics.Raycast(ray, out RaycastHit hit))
      return hit.point;

    return Vector3.zero;
  }
}
