using UnityEngine;
using UnityEngine.UI;
using ClashShared;

namespace ClashMeta
{
  public class DeckCardView : CardViewBase
  {
    [Header("Buttons")]
    [SerializeField] Button cardButton;
    [SerializeField] GameObject actionPanel;
    [SerializeField] Button infoButton;
    [SerializeField] Button removeButton;

    [Header("Replace Mode")]
    [SerializeField] GameObject replaceOverlay; // semi-transparent overlay + "Tap to replace" text

    int boundSlotIndex;
    CardId boundCardId;
    int boundLevel;

    void Awake()
    {
      if (cardButton != null) cardButton.onClick.AddListener(OnCardTapped);
      if (infoButton != null) infoButton.onClick.AddListener(OnInfo);
      if (removeButton != null) removeButton.onClick.AddListener(OnRemove);
      SetActionPanel(false);
    }

    void Update()
    {
      if (replaceOverlay != null)
        replaceOverlay.SetActive(DeckEditSession.IsActive);
    }

    void OnCardTapped()
    {
      if (DeckEditSession.IsActive)
      {
        DeckEditSession.ConfirmSlot(boundSlotIndex);
        DeckEditEvents.PickDeckSlot(boundSlotIndex);
        return;
      }
      SetActionPanel(actionPanel == null || !actionPanel.activeSelf);
    }

    public void ClosePanel() => SetActionPanel(false);

    void SetActionPanel(bool open)
    {
      if (actionPanel != null) actionPanel.SetActive(open);
      if (copiesBarContainer != null) copiesBarContainer.SetActive(!open);
      if (open) ClickOutsideDetector.Watch(actionPanel, () => SetActionPanel(false), safeZone: gameObject);
      else ClickOutsideDetector.Unwatch(actionPanel);
    }

    void OnInfo()
    {
      SetActionPanel(false);
      ShowInfo(boundCardId, boundLevel);
    }

    void OnRemove()
    {
      SetActionPanel(false);
      DeckEditEvents.RemoveFromDeck(boundSlotIndex);
    }

    public void Bind(DeckSlot slot, CardLibrary cardLibrary, int slotIndex)
    {
      boundSlotIndex = slotIndex;
      boundCardId = (CardId)slot.cardId;
      boundLevel = slot.level;
      SetActionPanel(false);
      if (icon != null) icon.color = Color.white;
      if (cardButton != null) cardButton.interactable = true;
      ApplyDisplay(boundCardId, cardLibrary, slot.level, slot.copies, slot.copiesRequired);
    }

    public void Clear()
    {
      SetActionPanel(false);
      ClearDisplay();
      if (icon != null) icon.color = new Color(0f, 0f, 0f, 80f / 255f);
      if (costText != null) costText.text = "0";
      if (cardButton != null) cardButton.interactable = false;
    }
  }
}
