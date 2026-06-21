using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

namespace ClashMeta
{
  public class BattlePanelController : MonoBehaviour
  {
    [Header("Refs")]
    [SerializeField] Button findMatchButton;
    [SerializeField] Button playWithBotButton;
    [SerializeField] MatchmakingOverlay matchmakingOverlay;

    [Header("Player Stats (optional)")]
    [SerializeField] TextMeshProUGUI trophiesText;
    [SerializeField] TextMeshProUGUI winRateText;

    void OnEnable()
    {
      Debug.Log("[BattlePanel] OnEnable");
      findMatchButton.onClick.AddListener(OnFindMatch);
      playWithBotButton.onClick.AddListener(OnPlayWithBot);
      StartCoroutine(LoadStats());
    }

    void OnDisable()
    {
      findMatchButton.onClick.RemoveListener(OnFindMatch);
      playWithBotButton.onClick.RemoveListener(OnPlayWithBot);
    }

    void OnPlayWithBot()
    {
      Debug.Log("[BattlePanel] OnPlayWithBot clicked");
      playWithBotButton.interactable = false;
      StartCoroutine(DoPlayWithBot());
    }

    IEnumerator DoPlayWithBot()
    {
      ushort[] deck = null;
      yield return PlayerApi.GetActiveDeck(d => deck = d);

      playWithBotButton.interactable = true;

      if (deck == null)
      {
        ToastNotification.Show("Must have a full 8-card deck set before queuing");
        yield break;
      }

      var nm = NetworkManager.singleton as MyNetworkManager;
      if (nm == null) yield break;

      nm.PendingMatchId = "";
      nm.PendingPlayerToken = "";
      nm.PendingDeck = deck;
      nm.StartClient();
    }

    void OnFindMatch()
    {
      SetFindMatchButtonState(false);
      StartCoroutine(DoFindMatch());
    }

    IEnumerator DoFindMatch()
    {
      yield return MatchmakingService.FindMatch(ok =>
      {
        SetFindMatchButtonState(true);
        if (ok) matchmakingOverlay.Show();
      });
    }

    IEnumerator LoadStats()
    {
      yield return PlayerApi.GetMe(profile =>
      {
        if (profile == null) return;
        if (trophiesText != null) trophiesText.text = profile.trophies.ToString();
      });
    }

    void SetFindMatchButtonState(bool interactable)
    {
      findMatchButton.interactable = interactable;
    }
  }
}
