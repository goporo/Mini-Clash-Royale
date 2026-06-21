using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ClashShared;

namespace ClashMeta
{
  public class CardInfoPopup : PopupBase
  {
    public static CardInfoPopup Instance { get; private set; }

    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] Transform statsContainer;
    [SerializeField] GameObject statRowPrefab;
    [SerializeField] GameObject statLinePrefab;
    [SerializeField] Button btnClose;
    [SerializeField] CardLibrary cardLibrary;

    readonly System.Collections.Generic.List<string> pendingStats = new();

    protected override void Awake()
    {
      base.Awake();
      Instance = this;
      if (btnClose != null) btnClose.onClick.AddListener(Hide);
    }

    public void Show(CardId cardId, int level = 1)
    {
      Show();
      // clear trước khi add row mới
      for (int i = statsContainer.childCount - 1; i >= 0; i--)
        DestroyImmediate(statsContainer.GetChild(i).gameObject);

      if (titleText != null)
        titleText.text = $"Level {level} {cardId}";

      var s = CardStatsTable.Get(cardId);
      int elixir = CardCostTable.GetMilliElixirCost(cardId) / 1000;

      AddRow($"Elixir: {elixir}");
      AddRow($"HP: {Mathf.RoundToInt(s.MaxHP)}");
      if (s.AttackDamage > 0)
        AddRow($"Damage: {Mathf.RoundToInt(s.AttackDamage)}");
      if (s.AttackCooldown > 0)
        AddRow($"Attack Speed: {s.AttackCooldown:F1}s");
      if (s.AttackRange > 0)
        AddRow($"Range: {s.AttackRange:F1}");
      if (s.MoveSpeed > 0)
        AddRow($"Speed: {MoveSpeedLabel(s.MoveSpeed)}");
      if (s.SplashRadius > 0)
        AddRow($"Splash Radius: {s.SplashRadius:F1}");
      if (s.OnHitSlowDuration > 0)
        AddRow($"Slow on Hit: {Mathf.RoundToInt(s.OnHitSlowMagnitude * 100)}% / {s.OnHitSlowDuration:F1}s");
      if (s.SpawnNovaDamage > 0)
        AddRow($"Spawn Nova Dmg: {Mathf.RoundToInt(s.SpawnNovaDamage)}");
      if (s.SpawnNovaSlowDuration > 0)
        AddRow($"Spawn Nova Slow: {Mathf.RoundToInt(s.SpawnNovaSlowMagnitude * 100)}% / {s.SpawnNovaSlowDuration:F1}s");
      if (s.DeathNovaRadius > 0)
        AddRow($"Death Nova Dmg: {Mathf.RoundToInt(s.DeathNovaDamage)}");
      if (s.DeathNovaSlowDuration > 0)
        AddRow($"Death Nova Slow: {Mathf.RoundToInt(s.DeathNovaSlowMagnitude * 100)}% / {s.DeathNovaSlowDuration:F1}s");

      FlushRow(); // xử lý stat lẻ cuối cùng nếu có
    }


    void AddRow(string text)
    {
      pendingStats.Add(text);
      if (pendingStats.Count == 2) FlushRow();
    }

    void FlushRow()
    {
      if (pendingStats.Count == 0) return;
      var line = Instantiate(statLinePrefab, statsContainer);
      foreach (var s in pendingStats)
      {
        var item = Instantiate(statRowPrefab, line.transform);
        var tmp = item.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = s;
      }
      // pad với empty item để giữ đúng nửa width khi chỉ có 1 stat
      bool needPad = pendingStats.Count == 1;
      pendingStats.Clear();
      if (needPad)
      {
        var placeholder = Instantiate(statRowPrefab, line.transform);
        foreach (var gr in placeholder.GetComponentsInChildren<Graphic>())
          gr.enabled = false;
      }
    }

    static string MoveSpeedLabel(float speed) => speed switch
    {
      <= 45f => "Slow",
      <= 60f => "Medium",
      <= 90f => "Fast",
      _ => "Very Fast"
    };
  }
}
