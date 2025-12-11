using UnityEngine;

public class PlayerSpawner
{
  private SpawnController spawner;
  private CardConfig card;
  private Board board;



  public PlayerSpawner(SpawnController spawner, CardConfig card, Board board)
  {
    this.spawner = spawner;
    this.card = card;
    this.board = board;
  }

  public bool CanSpawn(Vector3 worldPos)
  {
    return true;
    return board.IsValidSpawnPosition(
        worldPos,
        card.spawnRules,
        EntityTeam.Team1
    );
  }

  public void SpawnAt(Vector3 worldPos)
  {
    spawner.SpawnUnit(card.unitConfig, worldPos, EntityTeam.Team1);
  }

}
