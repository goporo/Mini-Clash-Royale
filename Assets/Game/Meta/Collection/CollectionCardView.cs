using UnityEngine;
using UnityEngine.UI;
using ClashShared;

namespace ClashMeta
{
  public class CollectionCardView : CardViewBase
  {
    [SerializeField] GameObject lockedOverlay;

    [Header("Buttons")]
    [SerializeField] Button btnCard;
    [SerializeField] GameObject actionPanel;
    [SerializeField] Button btnInfo;
    [SerializeField] Button btnUse;

    int boundCardId;
    int boundLevel;
    bool isLocked;


    void Awake()
    {
      if (btnCard != null) btnCard.onClick.AddListener(OnCardTapped);
      if (btnInfo != null) btnInfo.onClick.AddListener(OnInfo);
      if (btnUse != null) btnUse.onClick.AddListener(OnUse);
      CloseActionPanel();
    }

    void OnCardTapped()
    {
      if (isLocked) { ShowInfo((CardId)boundCardId, boundLevel); return; }
      bool isOpen = actionPanel != null && actionPanel.activeSelf;
      if (isOpen) CloseActionPanel();
      else OpenActionPanel();
    }

    void OpenActionPanel()
    {
      DeckEditSession.Cancel();
      if (actionPanel != null) actionPanel.SetActive(true);
      if (copiesBarContainer != null) copiesBarContainer.SetActive(false);
      ClickOutsideDetector.Watch(actionPanel, CloseActionPanel, safeZone: gameObject);
    }

    public void CloseActionPanel()
    {
      DeckEditSession.Cancel();
      if (actionPanel != null) actionPanel.SetActive(false);
      if (copiesBarContainer != null) copiesBarContainer.SetActive(true);
      ClickOutsideDetector.Unwatch(actionPanel);
    }

    void OnInfo()
    {
      CloseActionPanel();
      ShowInfo((CardId)boundCardId, boundLevel);
    }

    void OnUse()
    {
      CloseActionPanel();
      DeckEditSession.BeginPickSlot(boundCardId);
      DeckEditEvents.SelectCardFromCollection(boundCardId);
    }

    public void Bind(CollectionCard card, CardLibrary cardLibrary)
    {
      boundCardId = card.CardIdInt;
      boundLevel = card.level;
      isLocked = false;
      CloseActionPanel();
      if (lockedOverlay != null) lockedOverlay.SetActive(false);
      ApplyDisplay((CardId)card.CardIdInt, cardLibrary, card.level, card.copies, card.copiesRequired);
    }

    public void BindLocked(CardId cardId, CardLibrary cardLibrary)
    {
      boundCardId = (int)cardId;
      boundLevel = 1;
      isLocked = true;
      CloseActionPanel();
      if (lockedOverlay != null) lockedOverlay.SetActive(true);
      ApplyDisplay(cardId, cardLibrary, 0, 0, 0);
    }
  }
}
