using System.Collections;
using UnityEngine;

namespace ClashMeta
{
  // Attach to the Chest Container GameObject.
  // Wire up: slotCount (4), chestItemPrefab, rewardPopup.
  public class ChestContainerView : MonoBehaviour
  {
    const int SlotCount = 4;

    [SerializeField] ChestItemView chestItemPrefab;
    [SerializeField] Transform     slotsParent;
    [SerializeField] ChestRewardPopup rewardPopup;

    readonly ChestItemView[] slots = new ChestItemView[SlotCount];

    void OnEnable()  => StartCoroutine(LoadChests());
    void OnDisable() => DestroySlots();

    IEnumerator LoadChests()
    {
      DestroySlots();
      SpawnSlots();

      ChestListResponse response = null;
      yield return PlayerApi.GetChests(r => response = r);

      if (response?.chests == null)
      {
        for (int i = 0; i < SlotCount; i++) slots[i].MarkEmpty();
        yield break;
      }

      // Build slotIndex → chest map
      var map = new System.Collections.Generic.Dictionary<int, ChestData>();
      foreach (var c in response.chests)
        if (c.slotIndex >= 0 && c.slotIndex < SlotCount)
          map[c.slotIndex] = c;

      for (int i = 0; i < SlotCount; i++)
      {
        if (map.TryGetValue(i, out var chest))
          BindSlot(i, chest);
        else
          slots[i].MarkEmpty();
      }
    }

    void SpawnSlots()
    {
      for (int i = 0; i < SlotCount; i++)
      {
        slots[i] = Instantiate(chestItemPrefab, slotsParent);
        int idx = i;
        slots[i].OnRequestUnlock = view => StartCoroutine(DoUnlock(view));
        slots[i].OnRequestOpen   = view => StartCoroutine(DoOpen(view));
      }
    }

    void DestroySlots()
    {
      for (int i = 0; i < SlotCount; i++)
      {
        if (slots[i] != null) Destroy(slots[i].gameObject);
        slots[i] = null;
      }
    }

    void BindSlot(int slotIndex, ChestData chest)
    {
      if (slots[slotIndex] == null) return;
      slots[slotIndex].Bind(chest);
    }

    IEnumerator DoUnlock(ChestItemView view)
    {
      if (view.Data == null) yield break;

      StartUnlockResponse unlockResp = null;
      yield return PlayerApi.StartUnlockChest(view.Data.id, r => unlockResp = r);

      if (unlockResp == null)
      {
        Debug.LogWarning("[Chest] StartUnlock failed");
        yield break;
      }

      view.ApplyUnlockResponse(unlockResp);
    }

    IEnumerator DoOpen(ChestItemView view)
    {
      if (view.Data == null) yield break;

      OpenChestResponse openResp = null;
      yield return PlayerApi.OpenChest(view.Data.id, r => openResp = r);

      if (openResp == null)
      {
        Debug.LogWarning("[Chest] Open failed");
        yield break;
      }

      if (rewardPopup != null)
        rewardPopup.Show(openResp.rewards);

      // refresh the whole list so the opened slot clears
      StartCoroutine(LoadChests());
    }
  }
}
