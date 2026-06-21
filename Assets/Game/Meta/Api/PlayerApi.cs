using System;
using System.Collections;
using UnityEngine;

namespace ClashMeta
{
  [Serializable]
  public class PlayerProfile
  {
    public string id;
    public string username;
    public int gold;
    public int gems;
    public int trophies;
    public int xpLevel;
    public bool isGuest;
  }

  [Serializable]
  public class CollectionCard
  {
    public string cardId;
    public int level;
    public int copies;
    public int copiesRequired;

    public int CardIdInt => int.TryParse(cardId, out int v) ? v : 0;
  }

  [Serializable]
  public class CollectionResponse
  {
    public CollectionCard[] cards;
  }

  [Serializable]
  public class DeckSlot
  {
    public int slotIndex;
    public int cardId;
    public int level;
    public int copies;
    public int copiesRequired;
  }

  [Serializable]
  public class DeckEntry
  {
    public int deckIndex;
    public bool isActive;
    public DeckSlot[] deck;
  }

  [Serializable]
  public class DeckListResponse
  {
    public int activeDeckIndex;
    public DeckEntry[] decks;
  }

  public static class PlayerApi
  {
    public static IEnumerator GetMe(Action<PlayerProfile> onDone)
    {
      yield return MetaApiClient.Get<PlayerProfile>(ApiConstants.Player.Me, result =>
      {
        onDone(result.Success ? result.Data : null);
      });
    }

    public static IEnumerator GetCollection(Action<CollectionResponse> onDone)
    {
      yield return MetaApiClient.Get<CollectionResponse>(ApiConstants.Collection.Cards, result =>
      {
        onDone(result.Success ? result.Data : null);
      });
    }

    [Serializable] public class DeckCardRequest { public int slotIndex; public int cardId; }
    [Serializable] class UpdateDeckRequest { public DeckCardRequest[] cards; }
    [Serializable] class SetActiveDeckRequest { public int deckIndex; }

    public static IEnumerator UpdateDeck(DeckCardRequest[] cards, Action<bool> onDone)
    {
      string json = JsonUtility.ToJson(new UpdateDeckRequest { cards = cards });
      yield return MetaApiClient.Post<object>(ApiConstants.Deck.Update, json, result => onDone(result.Success));
    }

    public static IEnumerator SetActiveDeck(int deckIndex, Action<bool> onDone)
    {
      string json = JsonUtility.ToJson(new SetActiveDeckRequest { deckIndex = deckIndex });
      yield return MetaApiClient.Post<object>(ApiConstants.Deck.SetActive, json, result => onDone(result.Success));
    }

    public static IEnumerator GetDecks(Action<DeckListResponse> onDone)
    {
      yield return MetaApiClient.Get<DeckListResponse>(ApiConstants.Deck.Get, result =>
      {
        onDone(result.Success ? result.Data : null);
      });
    }

    public static IEnumerator GetActiveDeck(Action<ushort[]> onDone)
    {
      yield return MetaApiClient.Get<DeckListResponse>(ApiConstants.Deck.GetActive, result =>
      {
        if (!result.Success || result.Data?.decks == null) { onDone(null); return; }

        DeckEntry active = null;
        foreach (var d in result.Data.decks)
          if (d.isActive) { active = d; break; }

        if (active?.deck == null || active.deck.Length < 8) { onDone(null); return; }

        var cards = new ushort[8];
        foreach (var slot in active.deck)
          if (slot.slotIndex >= 0 && slot.slotIndex < 8)
            cards[slot.slotIndex] = (ushort)slot.cardId;

        onDone(cards);
      });
    }
  }
}
