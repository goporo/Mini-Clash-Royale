using UnityEngine;

public class CardSlotUI : MonoBehaviour
{
  public CardConfig Config;

  // Set the card's UI position
  public void SetUIPosition(Vector3 screenPos)
  {
    transform.position = screenPos;
  }

  // Set the card's UI scale
  public void SetScale(float scale)
  {
    transform.localScale = Vector3.one * scale;
  }
}
