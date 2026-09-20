using UnityEngine;

/// <summary>
/// RangedEnemy의 발사 순간 조준, 탄환 그림/충돌체 생성과 임시 스프라이트를 담당합니다.
/// 탄환 생성 이후의 이동·피해·반사는 ReflectProjectile이 담당합니다. 직접 부착하지 않습니다.
/// </summary>
public sealed partial class RangedEnemy
{
    private static Sprite fallbackProjectileSprite;

    private void Fire()
    {
        if (target == null || !target.gameObject.activeInHierarchy) return;

        // 준비 동작 때의 방향이 아니라 실제 발사 순간의 플레이어 위치를 사용합니다.
        // 이 방향은 투사체에 한 번만 전달하므로 이후 플레이어가 움직여도 추적하지 않습니다.
        Vector2 releaseAim = target.position - transform.position;
        if (releaseAim.sqrMagnitude > .0001f) shotDirection = releaseAim.normalized;
        Face(shotDirection);

        GameObject shot = CreateProjectileVisual();
        CreateProjectileCollider(shot);
        shot.AddComponent<ReflectProjectile>().Initialize(gameObject, shotDirection, projectileSpeed, projectileDamage,
            projectileRadius, projectileLifetime, maxBounces, false);
    }

    private GameObject CreateProjectileVisual()
    {
        var shot = new GameObject("Ranged Projectile");
        shot.transform.position = transform.position + (Vector3)(shotDirection * Mathf.Max(0f, muzzleDistance));
        var renderer = shot.AddComponent<SpriteRenderer>();
        renderer.sprite = projectileSprite != null ? projectileSprite : GetFallbackProjectile();
        renderer.color = projectileColor;
        renderer.sortingLayerID = visual.sortingLayerID;
        renderer.sortingOrder = visual.sortingOrder + 1;
        float diameter = Mathf.Max(.01f, projectileRadius) * 2;
        Vector2 size = renderer.sprite.bounds.size;
        shot.transform.localScale = new Vector3(diameter / Mathf.Max(.001f, size.x), diameter / Mathf.Max(.001f, size.y), 1);
        return shot;
    }

    private void CreateProjectileCollider(GameObject shot)
    {
        // Collider는 별도 자식에서 월드 크기를 유지합니다(직사각형 스프라이트도 원형 충돌).
        var hitbox = new GameObject("Projectile Collider");
        hitbox.transform.SetParent(shot.transform, false);
        hitbox.transform.localScale = new Vector3(1 / shot.transform.localScale.x, 1 / shot.transform.localScale.y, 1);
        var circle = hitbox.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = Mathf.Max(.01f, projectileRadius);
    }

    private static Sprite GetFallbackProjectile()
    {
        if (fallbackProjectileSprite != null) return fallbackProjectileSprite;

        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        fallbackProjectileSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
        return fallbackProjectileSprite;
    }
}
