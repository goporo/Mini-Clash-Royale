using DG.Tweening;
using ClashShared;
using UnityEngine;

public static class CombatFx
{
  public static void PlayProjectile(Vector3 start, Vector3 end, Color color, float duration = 0.14f, float arcHeight = 0.18f)
  {
    GameObject projectile = CreatePrimitive(PrimitiveType.Sphere, "Combat Projectile", color, 0.18f);
    projectile.transform.position = start;

    Sequence sequence = DOTween.Sequence();
    sequence.Append(projectile.transform.DOJump(end, arcHeight, 1, duration).SetEase(Ease.Linear));
    sequence.Join(projectile.transform.DOPunchScale(Vector3.one * 0.08f, duration, 1, 0f));
    sequence.OnComplete(() =>
    {
      PlayImpact(end, color, 0.18f, 0.12f);
      Object.Destroy(projectile);
    });
  }

  public static void PlayFireball(Vector3 target, EntityTeam team)
  {
    GameObject fireball = CreatePrimitive(PrimitiveType.Sphere, "Fireball FX", new Color(1f, 0.5f, 0.15f), 0.75f);

    Vector3 startOffset = team == EntityTeam.Team1
      ? new Vector3(-1.5f, 8f, -2f)
      : new Vector3(1.5f, 8f, 2f);

    fireball.transform.position = target + startOffset;

    Sequence sequence = DOTween.Sequence();
    sequence.Append(fireball.transform.DOMove(target + Vector3.up * 0.15f, 0.32f).SetEase(Ease.InQuad));
    sequence.Join(fireball.transform.DOScale(0.35f, 0.32f).SetEase(Ease.InQuad));
    sequence.OnComplete(() =>
    {
      PlayImpact(target, new Color(1f, 0.62f, 0.2f), 1.6f, 0.22f);
      PlayRing(target, new Color(1f, 0.78f, 0.38f), 2.6f, 0.28f);
      Object.Destroy(fireball);
    });
  }

  private static void PlayImpact(Vector3 worldPosition, Color color, float finalScale, float duration)
  {
    GameObject impact = CreatePrimitive(PrimitiveType.Sphere, "Impact FX", color, 0.18f);
    impact.transform.position = worldPosition + Vector3.up * 0.08f;

    Sequence sequence = DOTween.Sequence();
    sequence.Append(impact.transform.DOScale(finalScale, duration).SetEase(Ease.OutQuad));
    sequence.Join(DOTween.To(
      () => 1f,
      alpha => SetAlpha(impact, alpha),
      0f,
      duration).SetEase(Ease.OutQuad));
    sequence.OnComplete(() => Object.Destroy(impact));
  }

  private static void PlayRing(Vector3 worldPosition, Color color, float finalScale, float duration)
  {
    GameObject ring = CreatePrimitive(PrimitiveType.Cylinder, "Impact Ring", color, 0.1f);
    ring.transform.position = worldPosition + Vector3.up * 0.02f;
    ring.transform.localScale = new Vector3(0.35f, 0.02f, 0.35f);

    Sequence sequence = DOTween.Sequence();
    sequence.Append(ring.transform.DOScale(new Vector3(finalScale, 0.02f, finalScale), duration).SetEase(Ease.OutQuad));
    sequence.Join(DOTween.To(
      () => 0.7f,
      alpha => SetAlpha(ring, alpha),
      0f,
      duration).SetEase(Ease.OutQuad));
    sequence.OnComplete(() => Object.Destroy(ring));
  }

  private static GameObject CreatePrimitive(PrimitiveType primitiveType, string objectName, Color color, float uniformScale)
  {
    GameObject gameObject = GameObject.CreatePrimitive(primitiveType);
    gameObject.name = objectName;
    gameObject.transform.localScale = Vector3.one * uniformScale;

    Collider collider = gameObject.GetComponent<Collider>();
    if (collider != null)
      Object.Destroy(collider);

    Renderer renderer = gameObject.GetComponent<Renderer>();
    if (renderer != null)
    {
      renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
      renderer.receiveShadows = false;

      Material material = renderer.material;
      material.color = color;
      material.SetColor("_Color", color);
      material.SetColor("_BaseColor", color);
    }

    return gameObject;
  }

  private static void SetAlpha(GameObject gameObject, float alpha)
  {
    Renderer renderer = gameObject != null ? gameObject.GetComponent<Renderer>() : null;
    if (renderer == null)
      return;

    Color color = renderer.material.color;
    color.a = alpha;
    renderer.material.color = color;
    renderer.material.SetColor("_Color", color);
    renderer.material.SetColor("_BaseColor", color);
  }
}
