using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ClashMeta
{
  public class ChestItemView : MonoBehaviour
  {
    [Header("Display")]
    [SerializeField] Image             chestIcon;
    [SerializeField] TextMeshProUGUI   chestNameText;
    [SerializeField] TextMeshProUGUI   countdownText;

    [Header("Action Panel")]
    [SerializeField] GameObject        actionPanel;
    [SerializeField] Button            unlockButton;
    [SerializeField] Button            openButton;

    [Header("Chest Icons")]
    [SerializeField] Sprite[]          chestTypeSprites; // indexed by ChestType enum

    ChestData data;
    Coroutine countdownRoutine;

    public Action<ChestItemView> OnRequestOpen;
    public Action<ChestItemView> OnRequestUnlock;

    void Awake()
    {
      if (unlockButton != null) unlockButton.onClick.AddListener(OnUnlockClicked);
      if (openButton   != null) openButton.onClick.AddListener(OnOpenClicked);
    }

    void OnDestroy() => StopCountdown();

    public void Bind(ChestData chest)
    {
      data = chest;
      RefreshDisplay();
    }

    public void ApplyUnlockResponse(StartUnlockResponse response)
    {
      data.status               = response.status;
      data.unlockReadyAt        = response.unlockReadyAt;
      RefreshDisplay();
    }

    public void MarkEmpty()
    {
      data = null;
      StopCountdown();
      if (chestIcon      != null) chestIcon.enabled = false;
      if (chestNameText  != null) chestNameText.text = "";
      if (countdownText  != null) countdownText.gameObject.SetActive(false);
      if (actionPanel    != null) actionPanel.SetActive(false);
    }

    void RefreshDisplay()
    {
      StopCountdown();

      if (chestIcon != null)
      {
        chestIcon.enabled = true;
        int typeIdx = Mathf.Clamp((int)data.Type, 0, chestTypeSprites != null ? chestTypeSprites.Length - 1 : 0);
        if (chestTypeSprites != null && typeIdx < chestTypeSprites.Length)
          chestIcon.sprite = chestTypeSprites[typeIdx];
      }

      if (chestNameText != null)
        chestNameText.text = data.Type.ToString();

      bool ready     = data.Status == ChestStatus.Ready || (data.Status == ChestStatus.Unlocking && data.IsReadyByTime());
      bool unlocking = data.Status == ChestStatus.Unlocking && !data.IsReadyByTime();
      bool locked    = data.Status == ChestStatus.Locked;

      // Action panel: hidden only while actively unlocking
      SetActionPanel(!unlocking);

      if (locked)
      {
        // Show the total unlock duration as a preview
        ShowStaticDuration(TimeSpan.FromSeconds(data.unlockDurationSeconds));
      }
      else if (unlocking)
      {
        StartCountdown();
      }
      else // ready
      {
        if (countdownText != null) countdownText.gameObject.SetActive(false);
      }
    }

    void ShowStaticDuration(TimeSpan duration)
    {
      if (countdownText == null) return;
      countdownText.gameObject.SetActive(duration.TotalSeconds > 0);
      if (duration.TotalSeconds > 0)
        countdownText.text = FormatTime(duration);
    }

    void StartCountdown()
    {
      StopCountdown();
      countdownRoutine = StartCoroutine(CountdownRoutine());
    }

    void StopCountdown()
    {
      if (countdownRoutine == null) return;
      StopCoroutine(countdownRoutine);
      countdownRoutine = null;
    }

    IEnumerator CountdownRoutine()
    {
      if (countdownText != null) countdownText.gameObject.SetActive(true);

      while (true)
      {
        var remaining = data?.RemainingTime() ?? TimeSpan.Zero;

        if (remaining <= TimeSpan.Zero)
        {
          if (countdownText != null) countdownText.gameObject.SetActive(false);
          if (data != null) data.status = (int)ChestStatus.Ready;
          // Re-enable action panel now that unlock is done
          SetActionPanel(true);
          yield break;
        }

        if (countdownText != null)
          countdownText.text = FormatTime(remaining);

        yield return new WaitForSeconds(1f);
      }
    }

    static string FormatTime(TimeSpan t)
    {
      if (t.TotalHours >= 1)
        return $"{(int)t.TotalHours}h {t.Minutes:D2}m";
      if (t.TotalMinutes >= 1)
        return $"{t.Minutes}min {t.Seconds:D2}sec";
      return $"{t.Seconds}sec";
    }

    void SetActionPanel(bool open)
    {
      if (actionPanel == null) return;
      actionPanel.SetActive(open);

      if (!open) return;

      bool canOpen = data != null && (data.Status == ChestStatus.Ready || (data.Status == ChestStatus.Unlocking && data.IsReadyByTime()));
      if (unlockButton != null) unlockButton.gameObject.SetActive(!canOpen && data?.Status == ChestStatus.Locked);
      if (openButton   != null) openButton.gameObject.SetActive(canOpen);
    }

    void OnUnlockClicked() => OnRequestUnlock?.Invoke(this);
    void OnOpenClicked()   => OnRequestOpen?.Invoke(this);

    public ChestData Data => data;
  }
}
