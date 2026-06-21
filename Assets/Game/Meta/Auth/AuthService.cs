using System;
using System.Collections;
using UnityEngine;

namespace ClashMeta
{
  [Serializable] class GuestLoginRequest  { public string username; }
  [Serializable] class GuestLoginResponse { public string accessToken; public string refreshToken; }
  [Serializable] class RefreshRequest     { public string refreshToken; }
  [Serializable] class RefreshResponse    { public string accessToken; public string refreshToken; }

  public static class AuthService
  {
    const string KEY_REFRESH = "auth_refresh_token";
    const string KEY_USERNAME = "auth_username";

    public static bool HasSession => PlayerPrefs.HasKey(KEY_REFRESH);
    public static string SavedUsername => PlayerPrefs.GetString(KEY_USERNAME, "");
    public static bool IsReady { get; private set; }
    public static event Action OnSessionReady;

    // Lấy access token mới từ refresh token đã lưu
    public static IEnumerator RestoreSession(Action<bool> onDone)
    {
      string rf = PlayerPrefs.GetString(KEY_REFRESH, "");
      if (string.IsNullOrEmpty(rf)) { onDone(false); yield break; }

      string body = JsonUtility.ToJson(new RefreshRequest { refreshToken = rf });
      yield return MetaApiClient.Post<RefreshResponse>(ApiConstants.Auth.RefreshToken, body, result =>
      {
        if (result.Success && !string.IsNullOrEmpty(result.Data?.accessToken))
        {
          MetaApiClient.AccessToken = result.Data.accessToken;
          if (!string.IsNullOrEmpty(result.Data.refreshToken))
          {
            PlayerPrefs.SetString(KEY_REFRESH, result.Data.refreshToken);
            PlayerPrefs.Save();
          }
          IsReady = true;
          Debug.Log("[Auth] Session ready");
          OnSessionReady?.Invoke();
          onDone(true);
        }
        else if (result.StatusCode == 401 || result.StatusCode == 403)
        {
          // Token thực sự hết hạn hoặc bị revoke → xóa, cần login lại
          PlayerPrefs.DeleteKey(KEY_REFRESH);
          PlayerPrefs.Save();
          onDone(false);
        }
        else
        {
          // Network error hoặc server down → giữ token, báo fail để retry sau
          Debug.LogWarning($"[Auth] RestoreSession failed (status={result.StatusCode}), keeping refresh token");
          onDone(false);
        }
      });
    }

    // Guest login — chỉ gọi khi chưa có refresh token
    public static IEnumerator GuestLogin(string username, Action<bool, string> onDone)
    {
      string body = JsonUtility.ToJson(new GuestLoginRequest { username = username });
      yield return MetaApiClient.Post<GuestLoginResponse>(ApiConstants.Auth.GuestLogin, body, result =>
      {
        if (result.Success && !string.IsNullOrEmpty(result.Data?.accessToken))
        {
          MetaApiClient.AccessToken = result.Data.accessToken;
          PlayerPrefs.SetString(KEY_REFRESH, result.Data.refreshToken);
          PlayerPrefs.SetString(KEY_USERNAME, username);
          PlayerPrefs.Save();
          IsReady = true;
          OnSessionReady?.Invoke();
          onDone(true, null);
        }
        else
        {
          onDone(false, result.Error);
        }
      });
    }

    public static void Logout()
    {
      PlayerPrefs.DeleteKey(KEY_REFRESH);
      PlayerPrefs.DeleteKey(KEY_USERNAME);
      MetaApiClient.AccessToken = null;
    }
  }
}
