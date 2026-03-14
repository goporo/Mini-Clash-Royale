using UnityEngine;
using ClashShared;

[CreateAssetMenu(menuName = "Game/Domain/Card")]
public class CardConfig : ScriptableObject
{
  public CardId CardId;
  public int Cost;   // milli-elixir (e.g. 3000 = 3 elixir)
  public Sprite Icon;
  public SpawnFormation Formation = SpawnFormation.Single;
  public CardType CardType;
  public PlacementRule PlacementRule;

  [Header("Preview")]
  public GameObject GhostPrefab;
  public float Radius;
}
