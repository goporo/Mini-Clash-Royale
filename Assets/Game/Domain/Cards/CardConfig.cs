using UnityEngine;

[CreateAssetMenu(menuName = "Game/Domain/Card")]
public class CardConfig : ScriptableObject
{
  public UnitConfig unitConfig;
  public CardSpawnRules spawnRules;
}
