using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ClashShared;

namespace ClashMeta
{
  public abstract class CardViewBase : MonoBehaviour
  {
    [SerializeField] protected Image icon;
    [SerializeField] protected TextMeshProUGUI levelText;
    [SerializeField] protected TextMeshProUGUI costText;
    [SerializeField] protected Image copiesBar;
    [SerializeField] protected TextMeshProUGUI copiesText;
    [SerializeField] protected GameObject copiesBarContainer;


    protected void ApplyDisplay(CardId cardId, CardLibrary cardLibrary, int level, int copies, int copiesRequired)
    {
      var config = cardLibrary != null ? cardLibrary.Get(cardId) : null;

      if (icon != null)
        icon.sprite = config?.Icon;

      if (costText != null)
        costText.text = (CardCostTable.GetMilliElixirCost(cardId) / 1000).ToString();

      if (levelText != null)
        levelText.text = level > 0 ? $"Level {level}" : "Level 1";

      if (copiesBar != null)
        copiesBar.fillAmount = copiesRequired > 0 ? (float)copies / copiesRequired : 0f;

      if (copiesText != null)
        copiesText.text = copies == 0 && copiesRequired <= 0
          ? "Locked"
          : $"{copies}/{(copiesRequired > 0 ? copiesRequired : 1)}";
    }

    protected void ShowInfo(CardId cardId, int level = 1) { if (CardInfoPopup.Instance != null) CardInfoPopup.Instance.Show(cardId, level); }

    protected void ClearDisplay()
    {
      if (icon != null) icon.sprite = null;
      if (levelText != null) levelText.text = "";
      if (costText != null) costText.text = "";
      if (copiesBar != null) copiesBar.fillAmount = 0;
      if (copiesText != null) copiesText.text = "";
    }
  }
}
