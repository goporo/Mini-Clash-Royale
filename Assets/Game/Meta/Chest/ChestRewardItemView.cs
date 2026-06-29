using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ClashShared;

namespace ClashMeta
{
  // A single reward item displayed inside the chest-open popup.
  // Prefab needs: Image (icon), TextMeshProUGUI (count label e.g. "x32").
  public class ChestRewardItemView : MonoBehaviour
  {
    [SerializeField] Image            icon;
    [SerializeField] TextMeshProUGUI  countLabel;

    // Bind a card reward — looks up icon from CardLibrary
    public void Bind(ChestCardReward reward, CardLibrary library)
    {
      if (!int.TryParse(reward.cardId, out int idInt)) return;
      var cardId = (CardId)idInt;
      var config = library != null ? library.Get(cardId) : null;

      if (icon != null)
        icon.sprite = config != null ? config.Icon : null;

      if (countLabel != null)
        countLabel.text = $"x{reward.copies}";
    }

    // Bind a generic resource reward (gold, gems…) using a provided sprite
    public void BindResource(Sprite sprite, int amount, string prefix = "+")
    {
      if (icon != null)
        icon.sprite = sprite;

      if (countLabel != null)
        countLabel.text = $"{prefix}{amount}";
    }
  }
}
