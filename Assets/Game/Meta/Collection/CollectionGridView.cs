using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ClashShared;

namespace ClashMeta
{
  public class CollectionGridView : MonoBehaviour
  {
    [Header("Grid")]
    [SerializeField] CollectionCardView cardViewPrefab;
    [SerializeField] Transform gridParent;

    [Header("Header")]
    [SerializeField] TextMeshProUGUI foundText;

    [Header("Refs")]
    [SerializeField] CardLibrary cardLibrary;
    [SerializeField] ScrollRect scrollRect;

    static readonly CardId[] AllPlayableCards = CardIdHelper.GetAllPlayableCards();

    readonly List<CollectionCardView> spawnedViews = new();

    int _currentDeckIndex = -1;

    void OnEnable()
    {
      GameplayEvents.Subscribe<CardSelectedFromCollectionEvent>(OnCardSelectedForSwap);
      GameplayEvents.Subscribe<DeckUpdatedEvent>(OnDeckUpdated);
      GameplayEvents.Subscribe<DeckTabChangedEvent>(OnDeckTabChanged);
      StartCoroutine(Load());
    }

    void OnDisable()
    {
      GameplayEvents.Unsubscribe<CardSelectedFromCollectionEvent>(OnCardSelectedForSwap);
      GameplayEvents.Unsubscribe<DeckUpdatedEvent>(OnDeckUpdated);
      GameplayEvents.Unsubscribe<DeckTabChangedEvent>(OnDeckTabChanged);
      foreach (var v in spawnedViews)
        if (v != null) Destroy(v.gameObject);
      spawnedViews.Clear();
    }

    void OnDeckUpdated(DeckUpdatedEvent evt) => StartCoroutine(Load());

    void OnDeckTabChanged(DeckTabChangedEvent evt)
    {
      _currentDeckIndex = evt.DeckIndex;
      DeckEditSession.Cancel();
      Refresh();
    }

    void OnCardSelectedForSwap(CardSelectedFromCollectionEvent evt)
    {
      if (scrollRect != null)
        scrollRect.normalizedPosition = new Vector2(0, 1);
    }

    CollectionResponse _collectionData;
    DeckListResponse _deckData;

    IEnumerator Load()
    {
      yield return PlayerApi.GetCollection(result => _collectionData = result);
      yield return PlayerApi.GetDecks(result => _deckData = result);
      if (_currentDeckIndex < 0) _currentDeckIndex = _deckData?.activeDeckIndex ?? 0;
      Refresh();
    }

    void Refresh()
    {
      foreach (var v in spawnedViews)
        if (v != null) Destroy(v.gameObject);
      spawnedViews.Clear();
      Canvas.ForceUpdateCanvases();
      if (scrollRect != null) scrollRect.normalizedPosition = new Vector2(0, 1);

      var owned = new Dictionary<int, CollectionCard>();
      if (_collectionData?.cards != null)
        foreach (var c in _collectionData.cards)
          owned[c.CardIdInt] = c;

      var deckCardIds = new HashSet<int>();
      if (_deckData?.decks != null)
        foreach (var d in _deckData.decks)
          if (d.deckIndex == _currentDeckIndex && d.deck != null)
            foreach (var slot in d.deck)
              deckCardIds.Add(slot.cardId);

      if (foundText != null)
        foundText.text = $"Found: {owned.Count}/{AllPlayableCards.Length}";

      foreach (var cardId in AllPlayableCards)
      {
        if (deckCardIds.Contains((int)cardId)) continue;
        if (!owned.ContainsKey((int)cardId)) continue;
        var view = Instantiate(cardViewPrefab, gridParent);
        spawnedViews.Add(view);
        view.Bind(owned[(int)cardId], cardLibrary);
      }

      foreach (var cardId in AllPlayableCards)
      {
        if (deckCardIds.Contains((int)cardId)) continue;
        if (owned.ContainsKey((int)cardId)) continue;
        var view = Instantiate(cardViewPrefab, gridParent);
        spawnedViews.Add(view);
        view.BindLocked(cardId, cardLibrary);
      }
    }
  }
}
