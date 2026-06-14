using TMPro;
using UnityEngine;

public class BattleUI : MonoBehaviour
{
  public static BattleUI Instance { get; private set; }

  public ElixirContainer ElixirContainer;

  private void Awake()
  {
    Instance = this;
  }

  public void UpdateElixir(int current)
  {
    ElixirContainer?.UpdateElixir(current);
    BattleHand.Instance?.UpdateElixir(current);
  }
}
