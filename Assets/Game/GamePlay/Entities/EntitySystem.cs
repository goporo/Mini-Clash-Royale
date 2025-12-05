using System.Collections.Generic;

public class EntitySystem
{
  private List<Entity> entities = new List<Entity>();
  private List<Entity> removalBuffer = new List<Entity>();

  public List<Entity> Entities => entities;

  public void Add(Entity e)
  {
    entities.Add(e);
  }

  public void MarkForRemoval(Entity e)
  {
    removalBuffer.Add(e);
  }

  public void Cleanup()
  {
    foreach (var e in removalBuffer)
      entities.Remove(e);

    removalBuffer.Clear();
  }
}
