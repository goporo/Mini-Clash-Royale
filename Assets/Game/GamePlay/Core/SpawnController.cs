using UnityEngine;

public class SpawnController
{
  private GameplayDirector director;
  private EntityFactory factory;
  private EntityViewFactory viewFactory;

  public SpawnController(GameplayDirector director, EntityViewFactory viewFactory)
  {
    this.director = director;
    this.viewFactory = viewFactory;
    this.factory = new EntityFactory(director);
  }

  public Entity SpawnUnit(UnitConfig config, Vector3 position, EntityTeam team)
  {
    var stats = config.CreateStats();
    var entity = factory.CreateUnit(position, stats, config, team);

    entity.AddBehaviour(new MovementComponent(config.spawnDirection));
    entity.AddBehaviour(new CombatBehaviour());

    director.Register(entity);
    viewFactory.CreateView(entity);

    return entity;
  }

  public Entity SpawnBuilding(BuildingConfig config, Vector3 position, EntityTeam team)
  {
    var stats = config.CreateStats();
    var entity = factory.CreateBuilding(position, stats, config, team);

    entity.AddBehaviour(new TowerAttackBehaviour());
    // Buildings do NOT get MovementBehaviour

    director.Register(entity);
    viewFactory.CreateView(entity);

    return entity;
  }

}

