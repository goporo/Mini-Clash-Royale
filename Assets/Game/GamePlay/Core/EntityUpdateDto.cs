/// <summary>
/// Data Transfer Object for entity updates from server.
/// Must match the structure sent from your Colyseus server.
/// </summary>
[System.Serializable]
public class EntityUpdateDto
{
  public int id;
  public float x;
  public float y;
  public float hp;
  public float maxHp;
  public string type;      // "knight", "tower", etc.
  public int team;         // 0 = Team1, 1 = Team2
  public bool isAlive;
}
