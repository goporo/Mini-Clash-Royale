using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ClashMeta
{
  public class DeckGridView : MonoBehaviour
  {
    [Header("Card Grid")]
    [SerializeField] DeckCardView cardSlotPrefab;
    [SerializeField] Transform gridParent;
    [SerializeField] CardLibrary cardLibrary;

    readonly DeckCardView[] cardSlots = new DeckCardView[8];

    [Header("Deck Tabs")]
    [SerializeField] Button[] deckTabButtons;
    [SerializeField] TextMeshProUGUI[] deckTabLabels;
    [SerializeField] Color tabActiveColor = Color.white;
    [SerializeField] Color tabInactiveColor = Color.gray;

    [Header("Avg Elixir")]
    [SerializeField] TextMeshProUGUI avgElixirText;

    DeckListResponse data;
    int selectedDeckIndex;

    void OnEnable()
    {
      GameplayEvents.Subscribe<RemoveFromDeckEvent>(OnRemoveSlot);
      GameplayEvents.Subscribe<DeckSlotPickedEvent>(OnSlotPicked);
      GameplayEvents.Subscribe<CardSelectedFromCollectionEvent>(OnCardSelectedFromCollection);
      ClickOutsideDetector.Watch(gameObject, DeckEditSession.Cancel);
      StartCoroutine(LoadThenCheckSwap());
    }

    void OnDisable()
    {
      GameplayEvents.Unsubscribe<RemoveFromDeckEvent>(OnRemoveSlot);
      GameplayEvents.Unsubscribe<DeckSlotPickedEvent>(OnSlotPicked);
      GameplayEvents.Unsubscribe<CardSelectedFromCollectionEvent>(OnCardSelectedFromCollection);
      ClickOutsideDetector.Unwatch(gameObject);
      DestroySlots();
    }

    void SpawnSlots()
    {
      DestroySlots();
      for (int i = 0; i < cardSlots.Length; i++)
        cardSlots[i] = Instantiate(cardSlotPrefab, gridParent);
    }

    void DestroySlots()
    {
      for (int i = 0; i < cardSlots.Length; i++)
      {
        if (cardSlots[i] != null) Destroy(cardSlots[i].gameObject);
        cardSlots[i] = null;
      }
    }

    void OnCardSelectedFromCollection(CardSelectedFromCollectionEvent evt)
    {
      if (data?.decks == null || selectedDeckIndex >= data.decks.Length) return;
      var slots = data.decks[selectedDeckIndex].deck;

      var filledIndices = new HashSet<int>();
      if (slots != null)
        foreach (var s in slots) filledIndices.Add(s.slotIndex);

      if (filledIndices.Count >= cardSlots.Length) return; // deck full, let user pick slot to replace

      int emptySlotIndex = -1;
      for (int i = 0; i < cardSlots.Length; i++)
        if (!filledIndices.Contains(i)) { emptySlotIndex = i; break; }

      if (emptySlotIndex < 0) return;

      DeckEditSession.Cancel();
      StartCoroutine(ReplaceCard(evt.CardId, emptySlotIndex, success =>
      {
        if (!success) Debug.LogWarning("[Deck] Auto-fill failed");
      }));
    }

    void OnSlotPicked(DeckSlotPickedEvent evt)
    {
      if (!DeckEditSession.HasPendingSwap) return;
      int cardId = DeckEditSession.PendingCardId;
      int slotIndex = DeckEditSession.PendingSlotIndex;
      DeckEditSession.ConsumeSwap();
      StartCoroutine(ReplaceCard(cardId, slotIndex, success =>
      {
        if (!success) Debug.LogWarning("[Deck] Swap failed");
      }));
    }

    IEnumerator LoadThenCheckSwap()
    {
      yield return PlayerApi.GetDecks(result => data = result);
      if (data == null || data.decks == null || data.decks.Length == 0) yield break;

      selectedDeckIndex = data.activeDeckIndex;
      SpawnSlots();
      SetupTabs();
      ShowDeck(selectedDeckIndex);

      if (DeckEditSession.HasPendingSwap)
      {
        int cardId = DeckEditSession.PendingCardId;
        int slotIndex = DeckEditSession.PendingSlotIndex;
        DeckEditSession.ConsumeSwap();
        yield return ReplaceCard(cardId, slotIndex, success =>
        {
          if (!success) Debug.LogWarning("[Deck] Auto-swap failed");
        });
      }
    }

    void OnRemoveSlot(RemoveFromDeckEvent evt)
    {
      StartCoroutine(ReplaceCard((int)ClashShared.CardId.None, evt.SlotIndex, success =>
      {
        if (!success) Debug.LogWarning("[Deck] Remove failed");
      }));
    }

    void SetupTabs()
    {
      for (int i = 0; i < deckTabButtons.Length; i++)
      {
        if (deckTabButtons[i] == null) continue;
        int idx = i;
        deckTabButtons[i].onClick.RemoveAllListeners();
        deckTabButtons[i].onClick.AddListener(() => ShowDeck(idx));

        bool exists = i < data.decks.Length;
        deckTabButtons[i].gameObject.SetActive(exists);
        if (exists && i < deckTabLabels.Length && deckTabLabels[i] != null)
          deckTabLabels[i].text = (i + 1).ToString();
      }
    }

    void ShowDeck(int deckIndex)
    {
      if (data?.decks == null || deckIndex >= data.decks.Length) return;

      selectedDeckIndex = deckIndex;
      DeckEditEvents.NotifyDeckTabChanged(deckIndex);
      StartCoroutine(PlayerApi.SetActiveDeck(deckIndex, _ => { }));

      for (int i = 0; i < deckTabButtons.Length; i++)
      {
        if (deckTabButtons[i] == null) continue;
        var img = deckTabButtons[i].GetComponent<Image>();
        if (img != null)
          img.color = i == selectedDeckIndex ? tabActiveColor : tabInactiveColor;
      }

      var slots = data.decks[deckIndex].deck;
      var slotMap = new Dictionary<int, DeckSlot>();
      if (slots != null)
        foreach (var s in slots)
          slotMap[s.slotIndex] = s;

      for (int i = 0; i < cardSlots.Length; i++)
      {
        if (cardSlots[i] == null) continue;
        if (slotMap.TryGetValue(i, out var slot))
          cardSlots[i].Bind(slot, cardLibrary, i);
        else
          cardSlots[i].Clear();
      }

      UpdateAvgElixir(slots);
    }

    public IEnumerator ReplaceCard(int newCardId, int slotIndex, Action<bool> onDone)
    {
      if (data?.decks == null || selectedDeckIndex >= data.decks.Length) { onDone?.Invoke(false); yield break; }

      var slots = data.decks[selectedDeckIndex].deck ?? new DeckSlot[0];
      if (slotIndex < 0) { onDone?.Invoke(false); yield break; }

      var cards = new List<PlayerApi.DeckCardRequest>();
      foreach (var s in slots)
      {
        if (s.slotIndex == slotIndex) continue; // replaced or removed
        if (s.cardId > 0) cards.Add(new PlayerApi.DeckCardRequest { slotIndex = s.slotIndex, cardId = s.cardId });
      }
      if (newCardId > 0)
        cards.Add(new PlayerApi.DeckCardRequest { slotIndex = slotIndex, cardId = newCardId });

      bool success = false;
      yield return PlayerApi.UpdateDeck(cards.ToArray(), result => success = result);

      if (success)
      {
        yield return PlayerApi.GetDecks(result => data = result);
        ShowDeck(selectedDeckIndex);
        DeckEditEvents.NotifyDeckUpdated();
      }

      onDone?.Invoke(success);
    }

    void UpdateAvgElixir(DeckSlot[] slots)
    {
      if (avgElixirText == null || slots == null) return;

      float total = 0f;
      int count = 0;
      foreach (var s in slots)
      {
        var cost = ClashShared.CardCostTable.GetMilliElixirCost((ClashShared.CardId)s.cardId);
        total += cost / 1000f;
        count++;
      }

      avgElixirText.text = count > 0 ? $"{total / count:F1}" : "-";
    }
  }
}
