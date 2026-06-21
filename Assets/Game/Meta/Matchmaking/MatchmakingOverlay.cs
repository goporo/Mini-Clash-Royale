using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

namespace ClashMeta
{
  public class MatchmakingOverlay : MonoBehaviour
  {
    [SerializeField] GameObject panel;
    [SerializeField] Button cancelButton;
    [SerializeField] TextMeshProUGUI statusText;

    const float POLL_INTERVAL = 1f;
    Coroutine pollCoroutine;

    public void Show()
    {
      panel.SetActive(true);
      cancelButton.interactable = true;
      statusText.text = "Searching for opponent...";
      cancelButton.onClick.RemoveAllListeners();
      cancelButton.onClick.AddListener(OnCancel);
      pollCoroutine = StartCoroutine(PollLoop());
    }

    void OnCancel()
    {
      StopPolling();
      StartCoroutine(DoCancel());
    }

    IEnumerator DoCancel()
    {
      cancelButton.interactable = false;
      statusText.text = "Cancelling...";
      yield return MatchmakingService.CancelMatch(() =>
      {
        panel.SetActive(false);
      });
    }

    IEnumerator PollLoop()
    {
      while (true)
      {
        yield return new WaitForSeconds(POLL_INTERVAL);
        yield return MatchmakingService.PollStatus(status =>
        {
          if (status == null) return;

          if (status.status == "matched")
          {
            StopPolling();
            OnMatched(status);
          }
          else if (status.status == "not_queued")
          {
            StopPolling();
            panel.SetActive(false);
          }
          else
          {
            statusText.text = $"Searching... {(int)status.queuedSeconds}s";
          }
        });
      }
    }

    void OnMatched(MatchStatusResponse status)
    {
      statusText.text = $"Found! vs {status.opponent?.username}";
      cancelButton.interactable = false;
      ConnectToBattle(status);
    }

    void ConnectToBattle(MatchStatusResponse status)
    {
      var nm = NetworkManager.singleton as MyNetworkManager;
      if (nm == null) return;

      nm.networkAddress = status.battleAddress;
      if (Transport.active is PortTransport pt)
        pt.Port = (ushort)status.battlePort;

      nm.PendingMatchId = status.matchId;
      nm.PendingPlayerToken = status.playerToken;
      nm.PendingDeck = null;
      nm.StartClient();
    }

    void StopPolling()
    {
      if (pollCoroutine != null)
      {
        StopCoroutine(pollCoroutine);
        pollCoroutine = null;
      }
    }
  }
}
