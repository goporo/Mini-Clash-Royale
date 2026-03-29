using TMPro;
using UnityEngine;

public class BattleUI : MonoBehaviour
{
  public static BattleUI Instance { get; private set; }

  public ElixirContainer ElixirContainer;
  public BattleHand battleHand;

  private void Awake()
  {
    Instance = this;
  }

  public void UpdateElixir(int current)
  {
    ElixirContainer?.UpdateElixir(current);
    (battleHand != null ? battleHand : BattleHand.Instance)?.UpdateElixir(current);
  }
}
