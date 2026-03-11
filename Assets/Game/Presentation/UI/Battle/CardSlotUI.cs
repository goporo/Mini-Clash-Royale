using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardSlotUI : MonoBehaviour
{
  public CardConfig Config;

  [Header("Visuals (optional)")]
  public Image cardIcon;
  public TMP_Text cardCostText;

  public void SetCard(CardConfig config)
  {
    Config = config;
    if (!config) return;
    cardIcon.sprite = config.Icon != null ? config.Icon : null;
    cardCostText.text = config.Cost > 0 ? $"{config.Cost}" : "";
  }

  public void SetUIPosition(Vector3 screenPos)
  {
    transform.position = screenPos;
  }

  public void SetScale(float scale)
  {
    transform.localScale = Vector3.one * scale;
  }
}
