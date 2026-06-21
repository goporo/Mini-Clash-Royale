using System;
using System.Collections;
using UnityEngine;

namespace ClashMeta
{
  [Serializable]
  public class FindMatchResponse
  {
    public string status;        // "searching"
    public bool creatingMatch;
    public float queuedSeconds;
  }

  [Serializable]
  public class MatchStatusResponse
  {
    public string status;        // "searching" | "matched" | "not_queued"
    public float queuedSeconds;
    public string matchId;
    public string battleAddress;
    public int battlePort;
    public string playerToken;
    public OpponentInfo opponent;
  }

  [Serializable]
  public class OpponentInfo
  {
    public string id;
    public string username;
    public int trophies;
  }

  public static class MatchmakingService
  {
    public static IEnumerator FindMatch(Action<bool> onDone)
    {
      yield return MetaApiClient.Post<FindMatchResponse>(ApiConstants.Battle.FindMatch, null, result =>
      {
        onDone(result.Success);
      });
    }

    public static IEnumerator PollStatus(Action<MatchStatusResponse> onResult)
    {
      yield return MetaApiClient.Get<MatchStatusResponse>(ApiConstants.Battle.MatchStatus, result =>
      {
        onResult(result.Success ? result.Data : null);
      });
    }

    public static IEnumerator CancelMatch(Action onDone = null)
    {
      yield return MetaApiClient.Post<FindMatchResponse>(ApiConstants.Battle.CancelMatch, null, _ => onDone?.Invoke());
    }
  }
}
