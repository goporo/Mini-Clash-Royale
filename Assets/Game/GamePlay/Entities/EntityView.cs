using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EntityView : MonoBehaviour
{
  private int entityId;
  private Vector3 targetPosition;
  private int currentHealth;
  private int maxHealth;
  private float smoothingSpeed = 10f;

  [SerializeField] private GameObject healthBar;
  [SerializeField] private TMP_Text healthText;

  public void SetEntityId(int id)
  {
    entityId = id;
  }

  public void SetTargetPosition(Vector3 position)
  {
    targetPosition = position;
  }

  private void Update()
  {
    transform.position = Vector3.Lerp(
        transform.position, targetPosition, Time.deltaTime * smoothingSpeed);
  }

  public void SetHealth(int hp, int maxHp)
  {
    currentHealth = hp;
    maxHealth = maxHp;
    UpdateHealthUI(hp, maxHp);
  }

  public void UpdateHealthUI(int hp, int maxHp)
  {
    if (healthBar != null)
    {
      Image hbImage = healthBar.GetComponent<Image>();
      if (hbImage != null)
      {
        hbImage.fillAmount = (float)hp / maxHp;
      }
    }

    if (healthText != null)
    {
      healthText.text = $"{hp}";
    }
  }

  public int GetEntityId() => entityId;
}
