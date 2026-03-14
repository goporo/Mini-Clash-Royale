using UnityEngine;
using UnityEngine.EventSystems;
using ClashServer;
using ClashShared;

public class CardDragController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
  public CardSlotUI slot;
  public CardPlacementPreview preview;
  private PlayerNetwork playerNetwork;
  public SpawnOverlayView spawnOverlay;

  [Tooltip("Index of this slot in BattleHand.handSlots (0-3), for local UI cycling.")]
  public int slotIndex;

  public struct RegionBounds
  {
    public float Left;
    public float Right;
    public float Bottom;
    public float Top;
    public float RiverBottom;
    public float RiverTop;
  }

  RegionBounds bounds = new()
  {
    Left = -9f,
    Right = 9f,
    Bottom = -16f,
    Top = 16f,
    RiverBottom = -1f,
    RiverTop = 1f
  };


  private const float GRID_SIZE = ClientBoardState.GRID_SIZE;

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
    bool inField = worldPos != Vector3.zero && worldPos.z >= bounds.Bottom;

    if (inField)
    {
      // Card is fully gone — ghost takes over
      slot.SetScale(0f);
      if (!_previewActive)
      {
        preview.Show(slot.Config);
        _previewActive = true;
      }
      Vector2 snapped = ValidateAndSnapPosition(new Vector2(worldPos.x, worldPos.z), slot.Config.PlacementRule);
      preview.UpdatePosition(new Vector3(snapped.x, worldPos.y, snapped.y));
    }
    else
    {
      // Still in UI area: card follows cursor and shrinks toward the field boundary
      float fieldBottomScreenY = cam.WorldToScreenPoint(new Vector3(0f, 0f, bounds.Bottom)).y;
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

    if (!_previewActive || worldPos.z < bounds.Bottom)
    {
      preview.Hide();
      _previewActive = false;
      return;
    }

    Vector2 validated = ValidateAndSnapPosition(new Vector2(worldPos.x, worldPos.z), slot.Config.PlacementRule);
    Vector3 spawnPos = new(validated.x, 0.5f, validated.y);

    playerNetwork = PlayerNetwork.LocalPlayer;
    if (playerNetwork == null)
    {
      Debug.LogError("PlayerNetwork not found in scene!");
      preview.Hide();
      return;
    }

    playerNetwork.PlayCard(slot.Config.CardId, new Vector2(spawnPos.x, spawnPos.z));

    preview.Hide();
    _previewActive = false;
    // Optimistically update the slot locally; server confirms via CardDrawnMessage.
    BattleHand.Instance?.OnLocalCardPlayed(slotIndex);
  }

  private Vector2 ValidateAndSnapPosition(Vector2 pos, PlacementRule rule)
  {
    // Determine allowed Y bounds based on placement rule
    float minY = bounds.Bottom;
    float maxY = bounds.RiverBottom;
    if (rule == PlacementRule.Anywhere)
    {
      maxY = bounds.Top;
    }

    // Clamp cursor to the allowed region
    float wx = Mathf.Clamp(pos.x, bounds.Left, bounds.Right);
    float wy = Mathf.Clamp(pos.y, minY, maxY);

    // Cell bounds for the deploy region (cells whose CENTER is inside the region)
    int minCX = Mathf.FloorToInt((bounds.Left + GRID_SIZE * 0.5f) / GRID_SIZE);
    int maxCX = Mathf.FloorToInt((bounds.Right - GRID_SIZE * 0.5f) / GRID_SIZE);
    int minCY = Mathf.FloorToInt((minY + GRID_SIZE * 0.5f) / GRID_SIZE);
    int maxCY = Mathf.FloorToInt((maxY - GRID_SIZE * 0.5f) / GRID_SIZE);

    var (cx, cy) = ClientBoardState.WorldToCell(new Vector2(wx, wy));
    cx = Mathf.Clamp(cx, minCX, maxCX);
    cy = Mathf.Clamp(cy, minCY, maxCY);

    var (freeCX, freeCY) = FindNearestFreeCell(cx, cy, minCX, maxCX, minCY, maxCY, rule);
    return ClientBoardState.CellCenter(freeCX, freeCY);
  }

  // Search outward from (cx, cy) in Manhattan-distance rings until a free or valid cell is found.
  // For Anywhere cards, allows placement on occupied cells. For other rules, avoids them.
  private (int x, int y) FindNearestFreeCell(int cx, int cy, int minCX, int maxCX, int minCY, int maxCY, PlacementRule rule)
  {
    bool isAnywhere = rule == PlacementRule.Anywhere;

    if (!ClientBoardState.IsCellOccupied(cx, cy) || isAnywhere)
      return (cx, cy);

    for (int r = 1; r <= 8; r++)
    {
      (int x, int y)? best = null;
      float bestDistSq = float.MaxValue;

      for (int dx = -r; dx <= r; dx++)
        for (int dy = -r; dy <= r; dy++)
        {
          if (Mathf.Abs(dx) + Mathf.Abs(dy) != r) continue;
          int nx = cx + dx, ny = cy + dy;
          if (nx < minCX || nx > maxCX || ny < minCY || ny > maxCY) continue;
          if (!isAnywhere && ClientBoardState.IsCellOccupied(nx, ny)) continue;
          float d = dx * dx + dy * dy;
          if (d < bestDistSq) { bestDistSq = d; best = (nx, ny); }
        }

      if (best.HasValue) return best.Value;
    }

    return (cx, cy); // Fallback — server will reject if still on a building
  }

  private Vector3 GetWorldPos(Vector2 screenPos)
  {
    Ray ray = cam.ScreenPointToRay(screenPos);
    if (Physics.Raycast(ray, out RaycastHit hit))
      return hit.point;

    return Vector3.zero;
  }
}
