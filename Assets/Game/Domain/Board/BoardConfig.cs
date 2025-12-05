using UnityEngine;

[CreateAssetMenu(menuName = "Game/Board/BoardConfig")]
public class BoardConfig : ScriptableObject
{
  public float boardWidth = 16f;
  public float boardHeight = 30f;
  public float tileSize = 1f;

  public LaneInfo[] lanes;
}

[System.Serializable]
public struct LaneInfo
{
  public string laneId;
  public float xMin;
  public float xMax;
}