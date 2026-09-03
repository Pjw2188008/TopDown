using UnityEngine;

/// <summary>
/// 발사된 투사체 한 개의 이동, 수명, 충돌, 피해와 반사 방향을 처리하는 런타임 전용 컴포넌트입니다.
/// ProjectileEnemy가 투사체를 생성할 때 자동으로 추가하므로 다른 GameObject에 직접 부착하지 않습니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class ReflectProjectile : MonoBehaviour
{
    private GameObject owner;
    private Vector2 direction;
    private float speed;
    private float damage;
    private float radius;
    private float remainingLifetime;
    private int remainingBounces;
    private bool inheritsReflection;

    /// <summary>
    /// 투사체를 생성한 공격자와 이동·피해·반사 정보를 전달받아 초기화합니다.
    /// </summary>
    public void Initialize(
        GameObject projectileOwner,
        Vector2 launchDirection,
        float moveSpeed,
        float projectileDamage,
        float projectileRadius,
        float lifetime,
        int maxBounces,
        bool reflectFromOrdinarySurfaces)
    {
        owner = projectileOwner;
        direction = launchDirection.sqrMagnitude > 0f ? launchDirection.normalized : Vector2.right;
        speed = Mathf.Max(0f, moveSpeed);
        damage = Mathf.Max(0f, projectileDamage);
        radius = Mathf.Max(0.01f, projectileRadius);
        remainingLifetime = Mathf.Max(0.1f, lifetime);
        remainingBounces = Mathf.Max(0, maxBounces);
        inheritsReflection = reflectFromOrdinarySurfaces;
    }

    private void Update()
    {
        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        MoveAndCheckCollision(speed * Time.deltaTime);
    }

    private void MoveAndCheckCollision(float moveDistance)
    {
        Vector2 start = transform.position;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(start, radius, direction, moveDistance);
        RaycastHit2D closestHit = default;
        bool foundHit = false;

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null
                || hit.collider.gameObject == gameObject
                || IsOwnerCollider(hit.collider))
            {
                continue;
            }

            if (!foundHit || hit.distance < closestHit.distance)
            {
                closestHit = hit;
                foundHit = true;
            }
        }

        if (!foundHit)
        {
            transform.position = start + direction * moveDistance;
            return;
        }

        GameObject hitObject = closestHit.collider.gameObject;
        if (CombatDamageUtility.TryApplyDamage(hitObject, damage, owner))
        {
            Destroy(gameObject);
            return;
        }

        if (!CanReflectFrom(closestHit.collider) || remainingBounces <= 0)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 normal = closestHit.normal.sqrMagnitude > 0f
            ? closestHit.normal.normalized
            : -direction;

        direction = Vector2.Reflect(direction, normal).normalized;
        remainingBounces--;
        transform.position = closestHit.centroid + normal * 0.02f;
    }

    private bool CanReflectFrom(Collider2D hitCollider)
    {
        PasteTarget pasteTarget = hitCollider.GetComponentInParent<PasteTarget>();
        if (pasteTarget == null
            || (pasteTarget.TargetType != PasteTargetType.Object
                && pasteTarget.TargetType != PasteTargetType.Surface))
        {
            return false;
        }

        ReflectionErrorEffect reflection = hitCollider.GetComponentInParent<ReflectionErrorEffect>();
        return inheritsReflection || (reflection != null && reflection.IsActive);
    }

    private bool IsOwnerCollider(Collider2D hitCollider)
    {
        if (owner == null)
        {
            return false;
        }

        Transform hitTransform = hitCollider.transform;
        return hitTransform == owner.transform || hitTransform.IsChildOf(owner.transform);
    }
}
