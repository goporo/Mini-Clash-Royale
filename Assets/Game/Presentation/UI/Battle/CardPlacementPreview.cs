using UnityEngine;
using ClashShared;

public class CardPlacementPreview : MonoBehaviour
{
  [Header("Spell Preview")]
  [SerializeField] private GameObject spellRadiusRoot;
  [SerializeField] private Transform spellRadiusVisual;

  private GameObject ghostInstance;
  private CardConfig currentCard;

  public void Show(CardConfig card)
  {
    Hide();

    currentCard = card;

    if (card.CardType == CardType.Spell)
    {
      spellRadiusRoot.SetActive(true);

      float diameter = card.Radius * 2f;
      spellRadiusVisual.localScale = new Vector3(diameter, diameter, diameter);
    }
    else
    {
      if (card.GhostPrefab != null)
      {
        ghostInstance = Instantiate(card.GhostPrefab);
      }
      else
      {
        Debug.LogWarning($"No ghost prefab assigned for card {card.CardId}");
      }
    }
  }

  public void UpdatePosition(Vector3 pos, bool valid = true)
  {
    if (currentCard == null) return;

    if (currentCard.CardType == CardType.Spell)
    {
      if (spellRadiusRoot != null)
      {
        spellRadiusRoot.transform.position = new Vector3(pos.x, spellRadiusRoot.transform.position.y, pos.z);
      }
      EntityViewManager.Instance?.SetSpellHighlight(pos, currentCard.Radius);
    }
    else if (ghostInstance != null)
    {
      ghostInstance.transform.position = pos;
      SetGhostColor(valid ? Color.green : Color.red);
    }
  }

  public void Hide()
  {
    if (ghostInstance != null)
    {
      Destroy(ghostInstance);
      ghostInstance = null;
    }

    if (spellRadiusRoot != null)
    {
      spellRadiusRoot.SetActive(false);
    }

    EntityViewManager.Instance?.ClearSpellHighlight();
    currentCard = null;
  }

  private void SetGhostColor(Color color)
  {
    if (ghostInstance == null) return;

    var renderers = ghostInstance.GetComponentsInChildren<Renderer>();
    foreach (var r in renderers)
    {
      if (r.material.HasProperty("_Color"))
        r.material.color = color;
    }
  }

}