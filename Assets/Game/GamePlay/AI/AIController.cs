using UnityEngine;

public class AIController
{
  private SpawnController spawner;
  private float timer;
  private float spawnRate = 5f; // every x seconds
  private UnitConfig aiUnit;

  public AIController(SpawnController spawner, UnitConfig aiUnit)
  {
    this.spawner = spawner;
    this.aiUnit = aiUnit;
  }

  public void Tick(float dt)
  {
    timer += dt;
    if (timer >= spawnRate)
    {
      timer = 0;
      spawner.SpawnUnit(aiUnit, new Vector3(0, 1, 14), EntityTeam.Team2);
    }
  }
}
