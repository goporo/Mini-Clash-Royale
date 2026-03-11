using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ElixirContainer : MonoBehaviour
{
  [SerializeField] private GameObject elixirBar;
  [SerializeField] private TMP_Text elixirText;

  public void UpdateElixir(int current)
  {
    float elixirAmount = current / 1000f;
    if (elixirBar != null)
    {
      Image img = elixirBar.GetComponent<Image>();
      if (img != null)
      {
        img.fillAmount = elixirAmount / 10f;  // since max elixir is 10
      }
    }

    if (elixirText != null)
    {
      elixirText.text = $"{current / 1000f:F1}";
    }
  }
}