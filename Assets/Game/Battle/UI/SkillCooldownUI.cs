using UnityEngine;
using UnityEngine.UI;

public class SkillCooldownUI : MonoBehaviour
{
  [SerializeField] private Image cooldownOverlay;
  private float blendDuration = 0.12f;
  private float currentFillAmount;
  private float startFillAmount;
  private float targetFillAmount;
  private float blendElapsedTime;

  private void Awake()
  {
    if (cooldownOverlay == null)
      cooldownOverlay = GetComponent<Image>();
  }

  private void Update()
  {
    if (cooldownOverlay == null || Mathf.Approximately(currentFillAmount, targetFillAmount))
      return;

    blendElapsedTime += Time.deltaTime;
    float duration = Mathf.Max(0.0001f, blendDuration);
    float t = Mathf.Clamp01(blendElapsedTime / duration);
    currentFillAmount = Mathf.Lerp(startFillAmount, targetFillAmount, t);
    ApplyFill(currentFillAmount);
  }

  public void SetProgress(float progress01)
  {
    if (cooldownOverlay == null)
      return;

    float nextTargetFillAmount = 1f - Mathf.Clamp01(progress01);

    // Spending elixir should appear immediately, regen should ease smoothly.
    bool shouldSnapImmediately = nextTargetFillAmount > currentFillAmount;

    targetFillAmount = nextTargetFillAmount;

    if (shouldSnapImmediately)
    {
      currentFillAmount = targetFillAmount;
      startFillAmount = currentFillAmount;
      blendElapsedTime = 0f;
      ApplyFill(currentFillAmount);
      return;
    }

    startFillAmount = currentFillAmount;
    blendElapsedTime = 0f;
  }

  public void SetFromElixir(int currentMilliElixir, int costMilliElixir)
  {
    if (costMilliElixir <= 0)
    {
      SetProgress(1f);
      return;
    }

    float progress = currentMilliElixir / (float)costMilliElixir;
    SetProgress(progress);
  }

  public void SetReady()
  {
    SetProgress(1f);
  }

  public void SetEmpty()
  {
    SetProgress(0f);
  }

  private void ApplyFill(float fillAmount)
  {
    cooldownOverlay.fillAmount = fillAmount;
    cooldownOverlay.enabled = fillAmount > 0.001f;
  }
}
