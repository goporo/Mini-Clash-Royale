using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ElixirContainer : MonoBehaviour
{
  private const int MaxMilliElixir = 10000;

  [SerializeField] private GameObject elixirBar;
  [SerializeField] private Slider elixirSlider;
  [SerializeField] private Image elixirFillImage;
  [SerializeField] private TMP_Text elixirText;
  [SerializeField] private float increaseBlendDuration = 0.12f;

  private float currentFillAmount;
  private float targetFillAmount;
  private float blendStartFillAmount;
  private float blendElapsedTime;
  private int lastMilliElixir = -1;

  private void Awake()
  {
    if (elixirBar != null)
    {
      if (elixirSlider == null)
        elixirSlider = elixirBar.GetComponent<Slider>();

      if (elixirFillImage == null)
        elixirFillImage = elixirBar.GetComponent<Image>();
    }

    if (elixirSlider != null)
    {
      elixirSlider.minValue = 0f;
      elixirSlider.maxValue = 1f;
      currentFillAmount = elixirSlider.value;
      targetFillAmount = currentFillAmount;
      return;
    }

    if (elixirFillImage != null)
    {
      currentFillAmount = elixirFillImage.fillAmount;
      targetFillAmount = currentFillAmount;
    }
  }

  private void Update()
  {
    if (Mathf.Approximately(currentFillAmount, targetFillAmount))
      return;

    blendElapsedTime += Time.deltaTime;
    float duration = Mathf.Max(0.0001f, increaseBlendDuration);
    float t = Mathf.Clamp01(blendElapsedTime / duration);
    currentFillAmount = Mathf.Lerp(blendStartFillAmount, targetFillAmount, t);
    ApplyFillAmount(currentFillAmount);
  }

  public void UpdateElixir(int current)
  {
    float nextTargetFillAmount = Mathf.Clamp01(current / (float)MaxMilliElixir);
    bool shouldSnapImmediately = lastMilliElixir >= 0 && current < lastMilliElixir;

    targetFillAmount = nextTargetFillAmount;

    if (shouldSnapImmediately)
    {
      currentFillAmount = targetFillAmount;
      blendStartFillAmount = currentFillAmount;
      blendElapsedTime = 0f;
      ApplyFillAmount(currentFillAmount);
    }
    else
    {
      blendStartFillAmount = currentFillAmount;
      blendElapsedTime = 0f;
    }

    lastMilliElixir = current;

    if (elixirText != null)
    {
      elixirText.text = $"{Mathf.FloorToInt(current / 1000f)}";
    }
  }

  private void ApplyFillAmount(float fillAmount)
  {
    if (elixirSlider != null)
      elixirSlider.value = fillAmount;

    if (elixirFillImage != null)
      elixirFillImage.fillAmount = fillAmount;
  }
}
