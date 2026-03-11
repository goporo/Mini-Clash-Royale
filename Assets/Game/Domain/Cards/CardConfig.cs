using UnityEngine;
using ClashShared;

[CreateAssetMenu(menuName = "Game/Domain/Card")]
public class CardConfig : ScriptableObject
{
  public CardId CardId;
  public int Cost;   // milli-elixir (e.g. 3000 = 3 elixir)
  public Sprite Icon;
}
