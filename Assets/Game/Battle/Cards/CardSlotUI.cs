using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardSlotUI : MonoBehaviour
{
  public CardConfig Config;

  [Header("Visuals (optional)")]
  public Image cardIcon;
  public TMP_Text cardCostText;
  [SerializeField] private Image cardCostBackground;
  [SerializeField] private SkillCooldownUI cooldownOverlay;
  [SerializeField] private Color affordableColor = Color.white;
  [SerializeField] private Color unaffordableColor = new(0.45f, 0.45f, 0.45f, 1f);
  [SerializeField] private Color affordableCostColor;

  public void SetCard(CardConfig config)
  {
    Config = config;

    if (cardIcon != null)
      cardIcon.sprite = config != null && config.Icon != null ? config.Icon : null;

    if (cardCostText != null)
      cardCostText.text = config != null && config.Cost > 0 ? $"{GetDisplayCost(config.Cost)}" : "";

    if (config == null && cooldownOverlay != null)
      cooldownOverlay.SetReady();
  }

  public void SetUIPosition(Vector3 screenPos)
  {
    transform.position = screenPos;
  }

  public void SetScale(float scale)
  {
    transform.localScale = Vector3.one * scale;
  }

  public void SetAffordable(bool canAfford)
  {
    if (cardIcon != null)
      cardIcon.color = canAfford ? affordableColor : unaffordableColor;

    if (cardCostBackground != null)
      cardCostBackground.color = canAfford ? affordableCostColor : unaffordableColor;
  }

  public void RefreshState(int currentMilliElixir)
  {
    if (Config == null)
    {
      SetAffordable(false);

      if (cooldownOverlay != null)
        cooldownOverlay.SetReady();

      return;
    }

    int costMilliElixir = GetMilliElixirCost(Config);
    bool canAfford = currentMilliElixir >= costMilliElixir;

    SetAffordable(canAfford);

    if (cooldownOverlay != null)
      cooldownOverlay.SetFromElixir(currentMilliElixir, costMilliElixir);
  }

  private static int GetDisplayCost(int rawCost)
  {
    return rawCost <= 10 ? rawCost : rawCost / 1000;
  }

  private static int GetMilliElixirCost(CardConfig config)
  {
    if (config == null)
      return 0;

    return config.Cost <= 10 ? config.Cost * 1000 : config.Cost;
  }
}
