using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;

namespace ClashMeta
{
  public class ToastNotification : MonoBehaviour
  {
    public static ToastNotification Instance { get; private set; }

    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] TextMeshProUGUI messageText;
    [SerializeField] float displayDuration = 2f;
    [SerializeField] float fadeDuration = 0.3f;

    Coroutine _current;

    void Awake()
    {
      Instance = this;
      canvasGroup.alpha = 0;
      canvasGroup.blocksRaycasts = false;
    }

    public static void Show(string message)
    {
      if (Instance != null) Instance.ShowToast(message);
    }

    void ShowToast(string message)
    {
      messageText.text = message;
      if (_current != null) StopCoroutine(_current);
      _current = StartCoroutine(Run());
    }

    IEnumerator Run()
    {
      canvasGroup.DOKill();
      canvasGroup.blocksRaycasts = true;
      yield return canvasGroup.DOFade(1f, fadeDuration).WaitForCompletion();
      yield return new WaitForSeconds(displayDuration);
      yield return canvasGroup.DOFade(0f, fadeDuration).WaitForCompletion();
      canvasGroup.blocksRaycasts = false;
    }
  }
}
