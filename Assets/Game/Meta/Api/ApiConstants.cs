using System;
using System.IO;
using UnityEngine;

namespace ClashMeta
{
  [Serializable]
  class RuntimeConfig
  {
    public string metaBaseUrl;
  }

  public static class ApiConstants
  {
    public static string BaseUrl => LoadBaseUrl();

    static string LoadBaseUrl()
    {
      const string fallback = "https://flavor-absolute-window-rat.trycloudflare.com";
      try
      {
        string path = Path.Combine(Application.streamingAssetsPath, "config.json");
        if (!File.Exists(path)) return fallback;

        var config = JsonUtility.FromJson<RuntimeConfig>(File.ReadAllText(path));
        return string.IsNullOrEmpty(config?.metaBaseUrl) ? fallback : config.metaBaseUrl.TrimEnd('/');
      }
      catch (Exception e)
      {
        Debug.LogWarning($"[ApiConstants] Failed to load config.json, using fallback: {e.Message}");
        return fallback;
      }
    }

    public static class Auth
    {
      public const string GuestLogin = "/auth/guest-login";
      public const string RefreshToken = "/auth/refresh";
    }

    public static class Player
    {
      public const string Me = "/player/me";
    }

    public static class Collection
    {
      public const string Cards = "/collection/cards";
    }

    public static class Deck
    {
      public const string Get = "/deck";
      public const string GetActive = "/deck?active=true";
      public const string Update = "/deck/update";
      public const string SetActive = "/deck/active";
    }

    public static class Battle
    {
      public const string FindMatch = "/battle/find";
      public const string MatchStatus = "/battle/find/status";
      public const string CancelMatch = "/battle/find/cancel";
    }

    public static class Chests
    {
      public const string Get = "/chests";
      public static string StartUnlock(string chestId) => $"/chests/{chestId}/start-unlock";
      public static string Open(string chestId) => $"/chests/{chestId}/open";
    }
  }
}
