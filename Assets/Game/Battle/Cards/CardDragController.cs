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
    spawnOverlay.SetState(GetCurrentOverlayState());
  }

  private SpawnOverlayState GetCurrentOverlayState()
  {
    if (slot.Config.CardType == CardType.Spell)
      return SpawnOverlayState.None;
    return SpawnOverlayState.Full;
  }

  public void OnDrag(PointerEventData eventData)
  {
    // Raw raycast position — always in server world-space (camera never rotates).
    Vector3 rawWorldPos = GetRawWorldPos(eventData.position);

    bool inField = ClientCardPlacementService.TryGetPlacement(
        rawWorldPos, slot.Config.PlacementRule, out Vector2 snappedWorldPos);

    if (inField)
    {
      slot.SetScale(0f);
      if (!_previewActive)
      {
        preview.Show(slot.Config);
        _previewActive = true;
      }
      // Preview renders at visual-space position (mirrors back for Team2).
      Vector3 visualSnap = LocalPlayerContext.ToVisual(new Vector3(snappedWorldPos.x, rawWorldPos.y, snappedWorldPos.y));
      preview.UpdatePosition(visualSnap);
    }
    else
    {
      // The visual "entry edge" of the local player's deploy zone on screen.
      float deployEdgeWorldZ = LocalPlayerContext.IsTeam2 ? BattleArena.RiverTop : BattleArena.Bottom;
      float fieldEdgeScreenY = cam.WorldToScreenPoint(new Vector3(0f, 0f, deployEdgeWorldZ)).y;
      float t = Mathf.Clamp01(
          Mathf.InverseLerp(_cardInitialScreenPos.y, fieldEdgeScreenY, eventData.position.y));
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

    Vector3 rawWorldPos = GetRawWorldPos(eventData.position);

    if (!_previewActive ||
        !ClientCardPlacementService.TryGetPlacement(
            rawWorldPos, slot.Config.PlacementRule, out Vector2 serverWorldPos))
    {
      preview.Hide();
      _previewActive = false;
      return;
    }

    if (!ClientCardPlayService.TryPlayCard(slot.Config, serverWorldPos, slotIndex))
    {
      preview.Hide();
      return;
    }

    preview.Hide();
    _previewActive = false;
  }

  private Vector3 GetRawWorldPos(Vector2 screenPos)
  {
    Ray ray = cam.ScreenPointToRay(screenPos);
    if (Physics.Raycast(ray, out RaycastHit hit))
      return hit.point;

    return Vector3.zero;
  }
}
