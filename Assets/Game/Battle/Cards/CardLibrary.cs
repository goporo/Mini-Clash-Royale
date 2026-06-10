using UnityEngine;
using ClashShared;

[CreateAssetMenu(menuName = "Game/Card Library")]
public class CardLibrary : ScriptableObject
{
  public CardConfig[] Cards;

  public CardConfig Get(CardId cardId)
  {
    if (Cards == null) return null;
    foreach (CardConfig c in Cards)
      if (c != null && c.CardId == cardId) return c;
    return null;
  }
}
