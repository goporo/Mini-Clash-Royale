using System.Linq;

public class KingWakeBehaviour : IEntityBehaviour
{
  private Entity tower;
  private bool awakened = false;

  public KingWakeBehaviour(Entity tower) { this.tower = tower; }

  public void Tick(Entity e, float dt)
  {
    if (awakened) return;

    if (tower.Stats.CurrentHP < tower.Stats.MaxHP || tower.Director.Entities.Entities.Any(x => x.Team != tower.Team && x.IsBuilding == false))
    {
      awakened = true;
      e.AddBehaviour(new TowerAttackBehaviour());
    }
  }
}
