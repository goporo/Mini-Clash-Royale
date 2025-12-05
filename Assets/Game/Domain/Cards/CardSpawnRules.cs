using UnityEngine;

[CreateAssetMenu(menuName = "Game/Domain/CardSpawnRules")]
public class CardSpawnRules : ScriptableObject
{
  public bool restrictToOwnSide = true;
  public bool restrictToLanes = true;
  public float minDistanceFromEnemy = 0f;
}
