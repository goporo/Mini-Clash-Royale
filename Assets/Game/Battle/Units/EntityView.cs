using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EntityView : MonoBehaviour
{
  private static readonly int ColorId = Shader.PropertyToID("_Color");
  private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

  private int entityId;
  private Vector3 targetPosition;
  private Vector3 renderedPosition;
  private Vector3 attackOffset;
  private int currentHealth;
  private int maxHealth;
  private float smoothingSpeed = 10f;
  private float turnSpeed = 18f;
  private bool hasCombatTarget;
  private bool isDying;
  private Vector3 combatTargetPosition;
  private Vector3 initialScale;
  private Color baseColor = Color.white;
  private float flashBlend;

  private Image healthBarImage;
  private Renderer[] cachedRenderers;
  private Canvas[] cachedCanvases;
  private MaterialPropertyBlock propertyBlock;
  private Tween attackOffsetTween;
  private Tween attackScaleTween;
  private Tween hitFlashTween;
  private Tween deathScaleTween;
  private Tween deathOffsetTween;

  [SerializeField] private GameObject healthBar;
  [SerializeField] private TMP_Text healthText;

  private void Awake()
  {
    renderedPosition = transform.position;
    targetPosition = transform.position;
    initialScale = transform.localScale;
    cachedRenderers = GetComponentsInChildren<Renderer>(true);
    cachedCanvases = GetComponentsInChildren<Canvas>(true);
    propertyBlock = new MaterialPropertyBlock();
    healthBarImage = healthBar != null ? healthBar.GetComponent<Image>() : null;
    ApplyVisualColor();
  }

  public void SetEntityId(int id)
  {
    entityId = id;
  }

  public void SetTargetPosition(Vector3 position)
  {
    targetPosition = position;
  }

  private void Update()
  {
    if (!isDying)
    {
      renderedPosition = Vector3.Lerp(
          renderedPosition, targetPosition, Time.deltaTime * smoothingSpeed);
      transform.position = renderedPosition + attackOffset;
    }

    if (hasCombatTarget)
      FaceTowards(combatTargetPosition);
  }

  public void SetHealth(int hp, int maxHp)
  {
    currentHealth = hp;
    maxHealth = maxHp;
    UpdateHealthUI(hp, maxHp);
  }

  public void UpdateHealthUI(int hp, int maxHp)
  {
    if (healthBarImage != null)
      healthBarImage.fillAmount = maxHp > 0 ? (float)hp / maxHp : 0f;

    if (healthText != null)
      healthText.text = $"{hp}";
  }

  public void SetColor(Color color)
  {
    baseColor = color;

    if (healthBarImage != null)
      healthBarImage.color = color;

    ApplyVisualColor();
  }

  public void SetCombatTarget(Vector3 worldPosition)
  {
    hasCombatTarget = true;
    combatTargetPosition = worldPosition;
  }

  public void ClearCombatTarget()
  {
    hasCombatTarget = false;
  }

  public void PlaySpinAttack(float splashRadius)
  {
    if (isDying)
      return;

    attackOffsetTween?.Kill();
    attackScaleTween?.Kill();

    attackOffsetTween = DOTween.To(
        () => attackOffset,
        v => attackOffset = v,
        Vector3.up * 0.06f, 0.07f)
      .SetEase(Ease.OutQuad)
      .OnComplete(() =>
      {
        attackOffsetTween = DOTween.To(
            () => attackOffset,
            v => attackOffset = v,
            Vector3.zero, 0.15f)
          .SetEase(Ease.InQuad);
      });

    attackScaleTween = transform
      .DOPunchScale(Vector3.one * 0.15f, 0.22f, 2, 0f)
      .SetEase(Ease.OutQuad);

    DebugDrawSplashRadius(splashRadius);
  }

  private void DebugDrawSplashRadius(float radius)
  {
    const int segments = 32;
    const float duration = 0.4f;
    Vector3 center = transform.position;

    for (int i = 0; i < segments; i++)
    {
      float a0 = i / (float)segments * Mathf.PI * 2f;
      float a1 = (i + 1) / (float)segments * Mathf.PI * 2f;
      Vector3 p0 = center + new Vector3(Mathf.Cos(a0), 0.02f, Mathf.Sin(a0)) * radius;
      Vector3 p1 = center + new Vector3(Mathf.Cos(a1), 0.02f, Mathf.Sin(a1)) * radius;
      Debug.DrawLine(p0, p1, Color.yellow, duration);
    }
  }

  public void PlayAttack(Vector3 targetWorldPosition, bool isMelee)
  {
    if (isDying)
      return;

    SetCombatTarget(targetWorldPosition);

    Vector3 attackDirection = targetWorldPosition - renderedPosition;
    attackDirection.y = 0f;

    if (attackDirection.sqrMagnitude < 0.0001f)
      attackDirection = transform.forward;

    attackDirection.Normalize();

    attackOffsetTween?.Kill();
    attackScaleTween?.Kill();

    float attackDistance = isMelee ? 0.22f : 0.08f;
    float attackLift = isMelee ? 0.03f : 0.015f;

    attackOffsetTween = DOTween.To(
        () => attackOffset,
        value => attackOffset = value,
        attackDirection * attackDistance + Vector3.up * attackLift,
        isMelee ? 0.08f : 0.05f)
      .SetEase(Ease.OutQuad)
      .OnComplete(() =>
      {
        attackOffsetTween = DOTween.To(
            () => attackOffset,
            value => attackOffset = value,
            Vector3.zero,
            isMelee ? 0.12f : 0.08f)
          .SetEase(Ease.InQuad);
      });

    attackScaleTween = transform
      .DOPunchScale(Vector3.one * (isMelee ? 0.08f : 0.04f), isMelee ? 0.16f : 0.1f, 1, 0f)
      .SetEase(Ease.OutQuad);
  }

  public void PlayHitFlash()
  {
    if (isDying)
      return;

    hitFlashTween?.Kill();
    flashBlend = 0f;
    ApplyVisualColor();

    hitFlashTween = DOTween.Sequence()
      .Append(DOTween.To(() => flashBlend, value =>
      {
        flashBlend = value;
        ApplyVisualColor();
      }, 1f, 0.05f))
      .Append(DOTween.To(() => flashBlend, value =>
      {
        flashBlend = value;
        ApplyVisualColor();
      }, 0f, 0.09f))
      .SetEase(Ease.Linear);
  }

  public void PlayDeath(Action onComplete)
  {
    if (isDying)
      return;

    isDying = true;
    hasCombatTarget = false;

    attackOffsetTween?.Kill();
    attackScaleTween?.Kill();
    hitFlashTween?.Kill();

    foreach (Canvas canvas in cachedCanvases)
    {
      if (canvas != null)
        canvas.enabled = false;
    }

    flashBlend = 1f;
    ApplyVisualColor();

    deathScaleTween?.Kill();
    deathOffsetTween?.Kill();

    deathScaleTween = transform
      .DOScale(initialScale * 0.1f, 0.18f)
      .SetEase(Ease.InBack);

    deathOffsetTween = DOTween.To(
        () => attackOffset,
        value =>
        {
          attackOffset = value;
          transform.position = renderedPosition + attackOffset;
        },
        Vector3.down * 0.2f,
        0.18f)
      .SetEase(Ease.InQuad)
      .OnComplete(() => onComplete?.Invoke());
  }

  public int GetEntityId() => entityId;

  private void FaceTowards(Vector3 worldPosition)
  {
    Vector3 direction = worldPosition - transform.position;
    direction.y = 0f;

    if (direction.sqrMagnitude <= 0.0001f)
      return;

    Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    transform.rotation = Quaternion.Slerp(
      transform.rotation,
      targetRotation,
      Time.deltaTime * turnSpeed);
  }

  private void ApplyVisualColor()
  {
    if (cachedRenderers == null || propertyBlock == null)
      return;

    Color finalColor = Color.Lerp(baseColor, Color.white, flashBlend);

    foreach (Renderer renderer in cachedRenderers)
    {
      if (renderer == null)
        continue;

      renderer.GetPropertyBlock(propertyBlock);
      propertyBlock.SetColor(ColorId, finalColor);
      propertyBlock.SetColor(BaseColorId, finalColor);
      renderer.SetPropertyBlock(propertyBlock);
    }
  }

  private void OnDestroy()
  {
    attackOffsetTween?.Kill();
    attackScaleTween?.Kill();
    hitFlashTween?.Kill();
    deathScaleTween?.Kill();
    deathOffsetTween?.Kill();
  }
}
