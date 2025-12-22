using UnityEngine;

public class CardSlotUI : MonoBehaviour
{
  public CardConfig Config;

  public void SetUIPosition(Vector3 screenPos)
  {
    transform.position = screenPos;
  }

  public void SetScale(float scale)
  {
    transform.localScale = Vector3.one * scale;
  }
}
