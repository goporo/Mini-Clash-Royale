using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace ClashMeta
{
  public class GuestLoginPopup : MonoBehaviour
  {
    [SerializeField] TMP_InputField usernameInput;
    [SerializeField] Button startButton;
    [SerializeField] TextMeshProUGUI errorText;

    Action onSuccess;

    public void Show(Action onDone)
    {
      onSuccess = onDone;
      gameObject.SetActive(true);
      errorText.text = "";
      usernameInput.text = "";
      UpdateButtonState();
    }

    void OnEnable()
    {
      usernameInput.onValueChanged.RemoveAllListeners();
      startButton.onClick.RemoveAllListeners();
      usernameInput.onValueChanged.AddListener(_ => UpdateButtonState());
      startButton.onClick.AddListener(OnSubmit);
      UpdateButtonState();
    }

    void UpdateButtonState()
    {
      startButton.interactable = usernameInput.text.Trim().Length > 0;
    }

    void OnSubmit()
    {
      string username = usernameInput.text.Trim();
      if (string.IsNullOrEmpty(username)) return;

      SetLoading(true);
      StartCoroutine(DoLogin(username));
    }

    IEnumerator DoLogin(string username)
    {
      yield return AuthService.GuestLogin(username, (ok, error) =>
      {
        if (ok)
        {
          gameObject.SetActive(false);
          onSuccess?.Invoke();
        }
        else
        {
          errorText.text = error ?? "Login failed. Try again.";
          SetLoading(false);
        }
      });
    }

    void SetLoading(bool loading)
    {
      startButton.interactable = !loading;
      usernameInput.interactable = !loading;
      if (loading) errorText.text = "";
    }
  }
}
