using UnityEngine;
using ClashShared;

[CreateAssetMenu(menuName = "Game/Domain/Card")]
public class CardConfig : ScriptableObject
{
  public CardId CardId;
  public int Cost;   // milli-elixir (e.g. 3000 = 3 elixir, Just for display purposes, server doesn't use this)
  public Sprite Icon;
  public CardType CardType;
  public PlacementRule PlacementRule;

  [Header("Entity Visual")]
  public GameObject EntityPrefab;

  [Header("Placement Preview")]
  public GameObject GhostPrefab;
  public float Radius;
}
