using ClashServer;
using Mirror;
using UnityEngine;

public class EntityView : NetworkBehaviour
{

  public ServerEntity Entity { get; private set; }

  // Server-driven state
  private int entityId;
  private Vector3 targetPosition;
  private float currentHealth;
  private float maxHealth;
  private float smoothingSpeed = 10f;


  public void Bind(ServerEntity entity)
  {
    Entity = entity;
  }

  private void HandleEntityDeath()
  {
    Destroy(gameObject);
  }

  public void SetEntityId(int id)
  {
    entityId = id;
  }
  private void Update()
  {
    if (isClient)
    {
      transform.position = Vector3.Lerp(
          transform.position, targetPosition, Time.deltaTime * smoothingSpeed);
    }
  }
  public void SetHealth(float hp, float maxHp)
  {
    currentHealth = hp;
    maxHealth = maxHp;
    // TODO: Update health bar UI here
  }

  // void OnDrawGizmos()
  // {
  //   if (Entity != null && Entity.Stats != null)
  //   {
  //     Gizmos.color = Color.yellow;
  //     Gizmos.DrawWireSphere(transform.position, Entity.Stats.AttackRange);
  //   }
  // }
}
