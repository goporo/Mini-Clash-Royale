using UnityEngine;

public class EntityView : MonoBehaviour
{
  public Entity Entity { get; private set; }

  // Server-driven state
  private int entityId;
  private Vector3 targetPosition;
  private float currentHealth;
  private float maxHealth;
  private float smoothingSpeed = 10f; // Adjust for smoother/faster movement

  public void Bind(Entity entity)
  {
    Entity = entity;
    Entity.OnDeathEvent += HandleEntityDeath;
  }

  private void HandleEntityDeath()
  {
    Destroy(gameObject);
  }

  /// <summary>
  /// Set the entity ID for this view (used by server-driven mode).
  /// </summary>
  public void SetEntityId(int id)
  {
    entityId = id;
  }

  /// <summary>
  /// Set the target position from server update.
  /// The view will smoothly interpolate to this position.
  /// </summary>
  public void SetTargetPosition(float x, float y)
  {
    targetPosition = new Vector3(x, y, transform.position.z);
  }

  /// <summary>
  /// Set health values from server update.
  /// </summary>
  public void SetHealth(float hp, float maxHp)
  {
    currentHealth = hp;
    maxHealth = maxHp;
    // TODO: Update health bar UI here
  }

  /// <summary>
  /// Update visual representation (called by EntityViewManager).
  /// Smoothly interpolates to target position.
  /// </summary>
  public void UpdateVisual(float dt)
  {
    // Smooth interpolation to target position
    transform.position = Vector3.Lerp(transform.position, targetPosition, dt * smoothingSpeed);

    // TODO: Update animations based on movement/state
  }

  void Update()
  {
    // Legacy mode: if bound to a local entity, follow its position directly
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
