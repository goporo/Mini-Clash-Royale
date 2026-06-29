using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ClashMeta
{
  // Popup shown after a chest is opened.
  // Prefab structure:
  //   Root (CanvasGroup) → Panel → [title, goldRow, gemsRow, gridParent, btnClose]
  //   gridParent contains ChestRewardItemView prefabs (icon + count label)
  //   goldRow / gemsRow are optional rows with Image + TextMeshProUGUI
  public class ChestRewardPopup : PopupBase
  {
    public static ChestRewardPopup Instance { get; private set; }

    [Header("References")]
    [SerializeField] Transform              gridParent;
    [SerializeField] ChestRewardItemView    rewardItemPrefab;
    [SerializeField] Button                 btnClose;
    [SerializeField] CardLibrary            cardLibrary;

    [Header("Resource Icons (optional)")]
    [SerializeField] Sprite                 goldSprite;

    [Header("Summary Row (optional)")]
    [SerializeField] TextMeshProUGUI        goldText;

    protected override void Awake()
    {
      base.Awake();
      Instance = this;
      if (btnClose != null) btnClose.onClick.AddListener(Hide);
    }

    public void Show(ChestRewards rewards)
    {
      ClearGrid();

      // Summary text rows
      if (goldText != null)
        goldText.text = rewards.gold > 0 ? $"+{rewards.gold}" : "";

      // Gold item in grid
      if (goldSprite != null && rewards.gold > 0)
        SpawnResource(goldSprite, rewards.gold);

      // Card reward items
      if (rewards.cards != null)
      {
        foreach (var card in rewards.cards)
          SpawnCardItem(card);
      }

      Show(); // PopupBase.Show
    }

    void ClearGrid()
    {
      for (int i = gridParent.childCount - 1; i >= 0; i--)
        DestroyImmediate(gridParent.GetChild(i).gameObject);
    }

    void SpawnCardItem(ChestCardReward reward)
    {
      if (rewardItemPrefab == null) return;
      var item = Instantiate(rewardItemPrefab, gridParent);
      item.Bind(reward, cardLibrary);
    }

    void SpawnResource(Sprite sprite, int amount)
    {
      if (rewardItemPrefab == null) return;
      var item = Instantiate(rewardItemPrefab, gridParent);
      item.BindResource(sprite, amount);
    }
  }
}
