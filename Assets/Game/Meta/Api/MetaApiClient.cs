using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace ClashMeta
{
  public class ApiResult<T>
  {
    public bool Success;
    public T Data;
    public string Error;
    public int StatusCode;
  }

  public static class MetaApiClient
  {
    public static string AccessToken { get; set; }

    public static IEnumerator Get<T>(string path, Action<ApiResult<T>> onDone)
    {
      yield return Send<T>(UnityWebRequest.Get(ApiConstants.BaseUrl + path), onDone);
    }

    public static IEnumerator Post<T>(string path, string jsonBody, Action<ApiResult<T>> onDone)
    {
      var req = new UnityWebRequest(ApiConstants.BaseUrl + path, "POST");
      if (!string.IsNullOrEmpty(jsonBody))
      {
        byte[] body = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.SetRequestHeader("Content-Type", "application/json");
      }
      req.downloadHandler = new DownloadHandlerBuffer();
      yield return Send<T>(req, onDone);
    }

    static IEnumerator Send<T>(UnityWebRequest req, Action<ApiResult<T>> onDone)
    {
      if (req.url.Contains(".localto.net"))
        req.SetRequestHeader("localtonet-skip-warning", "1");

      bool isAuthRequest = req.url.Contains("/auth/");
      if (!isAuthRequest && !AuthService.IsReady)
      {
        Debug.Log($"[API] Waiting for auth... {req.url}");
        while (!AuthService.IsReady)
          yield return null;
      }

      if (!string.IsNullOrEmpty(AccessToken))
        req.SetRequestHeader("Authorization", $"Bearer {AccessToken}");

      Debug.Log($"[API] {req.method} {req.url}");
      yield return req.SendWebRequest();

      var result = new ApiResult<T>
      {
        StatusCode = (int)req.responseCode,
        Success = req.result == UnityWebRequest.Result.Success,
      };

      string rawBody = req.downloadHandler?.text;
      if (result.Success)
      {
        Debug.Log($"[API] {req.responseCode} {req.url} → {rawBody}");
        try { result.Data = JsonUtility.FromJson<T>(rawBody); }
        catch (Exception e) { result.Success = false; result.Error = e.Message; }
      }
      else
      {
        result.Error = TryParseErrorMessage(rawBody) ?? req.error ?? rawBody;
        if (result.StatusCode >= 500 || result.StatusCode == 0)
          Debug.LogWarning($"[API] {req.responseCode} {req.url} → ERR: {result.Error} | body: {rawBody}");
        else
          Debug.Log($"[API] {req.responseCode} {req.url} → {result.Error}");
        if (!string.IsNullOrEmpty(result.Error)) ToastNotification.Show(result.Error);
      }

      onDone?.Invoke(result);
    }

    [Serializable] class ErrorBody { public string error; }

    static string TryParseErrorMessage(string json)
    {
      if (string.IsNullOrEmpty(json)) return null;
      try { var e = JsonUtility.FromJson<ErrorBody>(json); return e?.error; }
      catch { return null; }
    }
  }
}
