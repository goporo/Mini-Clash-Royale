using System.Collections;
using UnityEngine;
using TMPro;

namespace ClashMeta
{
  public class TopBarView : MonoBehaviour
  {
    [SerializeField] TextMeshProUGUI playerName;
    [SerializeField] TextMeshProUGUI playerLevel;
    [SerializeField] TextMeshProUGUI gold;
    [SerializeField] TextMeshProUGUI gems;
    [SerializeField] TextMeshProUGUI trophies;


    public void Refresh() => StartCoroutine(Load());

    IEnumerator Load()
    {
      yield return PlayerApi.GetMe(profile =>
      {
        if (profile == null) return;
        Bind(profile);
      });
    }

    void Bind(PlayerProfile p)
    {
      if (playerName != null) playerName.text = p.username;
      if (playerLevel != null) playerLevel.text = $"Level {p.xpLevel}";
      if (gold != null) gold.text = $"Gold: {p.gold}";
      if (gems != null) gems.text = $"Gems: {p.gems}";
      if (trophies != null) trophies.text = $"Trophies: {p.trophies}";
    }
  }
}
