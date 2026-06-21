using UnityEngine;

namespace ClashMeta
{
  [RequireComponent(typeof(CanvasGroup))]
  public abstract class PopupBase : MonoBehaviour
  {
    [SerializeField] GameObject content; // click outside this closes the popup

    CanvasGroup canvasGroup;

    protected virtual void Awake()
    {
      canvasGroup = GetComponent<CanvasGroup>();
      HideImmediate();
    }

    public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0;

    protected void Show()
    {
      canvasGroup.alpha = 1;
      canvasGroup.blocksRaycasts = true;
      canvasGroup.interactable = true;
      ClickOutsideDetector.Watch(content, Hide);
    }

    public virtual void Hide()
    {
      canvasGroup.alpha = 0;
      canvasGroup.blocksRaycasts = false;
      canvasGroup.interactable = false;
      ClickOutsideDetector.Unwatch(content);
    }

    void HideImmediate()
    {
      canvasGroup.alpha = 0;
      canvasGroup.blocksRaycasts = false;
      canvasGroup.interactable = false;
    }
  }
}
