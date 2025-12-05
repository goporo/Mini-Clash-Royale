using UnityEngine;

public class CardPlacementPreview : MonoBehaviour
{
  public GameObject ghostPrefab;
  private GameObject ghost;
  private CardConfig currentCard;

  private PlayerSpawner playerSpawner;

  public void Init(PlayerSpawner spawner)
  {
    playerSpawner = spawner;
  }

  public void Show(CardConfig card)
  {
    currentCard = card;
    ghost = Instantiate(ghostPrefab);
  }

  public void UpdatePosition(Vector3 pos)
  {
    if (ghost == null) return;

    ghost.transform.position = pos;

    bool valid = playerSpawner.CanSpawn(pos);
    ghost.GetComponent<Renderer>().material.color = valid ? Color.green : Color.red;
  }

  public bool CanSpawnAt(Vector3 pos)
      => playerSpawner.CanSpawn(pos);

  public void Spawn(Vector3 pos)
  {
    playerSpawner.SpawnAt(pos);
  }

  public void Hide()
  {
    if (ghost != null) Destroy(ghost);
    ghost = null;
    currentCard = null;
  }
}
