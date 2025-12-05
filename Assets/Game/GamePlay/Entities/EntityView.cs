using UnityEngine;

public class EntityView : MonoBehaviour
{
  public Entity Entity { get; private set; }

  public void Bind(Entity entity)
  {
    Entity = entity;
    Entity.OnDeathEvent += HandleEntityDeath;
  }

  private void HandleEntityDeath()
  {
    Destroy(gameObject);
  }

  void Update()
  {
    if (Entity != null)
      transform.position = Entity.Position;
  }

  void OnDrawGizmos()
  {
    if (Entity != null && Entity.Stats != null)
    {
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(transform.position, Entity.Stats.AttackRange);
    }

  }
}
