using UnityEngine;

public class EntityFactory
{
  private int nextId = 1;

  private GameplayDirector director;

  public EntityFactory(GameplayDirector director)
  {
    this.director = director;
  }

  public Entity CreateUnit(Vector3 spawnPos, EntityStats stats, EntityConfigBase config, EntityTeam team)
  {
    return new Entity(nextId++, spawnPos, stats, config, team, director, false);
  }

  public Entity CreateBuilding(Vector3 spawnPos, EntityStats stats, EntityConfigBase config, EntityTeam team)
  {
    return new Entity(nextId++, spawnPos, stats, config, team, director, true);
  }
}

