using UnityEngine;

/// <summary>
/// 발사된 투사체 한 개의 이동, 수명, 충돌, 피해와 반사 방향을 처리하는 런타임 전용 컴포넌트입니다.
/// RangedEnemy가 투사체를 생성할 때 자동으로 추가하므로 다른 GameObject에 직접 부착하지 않습니다.
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
    private bool wasParried;

    /// <summary>실제 패링으로 해당 플레이어에게 소유권이 넘어간 탄환인지 확인합니다. 표면 반사는 포함하지 않습니다.</summary>
    public bool WasParriedBy(PlayerMove player) => wasParried && player != null && owner == player.gameObject;

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
        wasParried = false;
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
        if (!TryFindClosestHit(start, moveDistance, out RaycastHit2D closestHit))
        {
            transform.position = start + direction * moveDistance;
            return;
        }

        GameObject hitObject = closestHit.collider.gameObject;
        PlayerMove player = hitObject.GetComponentInParent<PlayerMove>();
        if (damage > 0f && player != null && player.TryParryProjectile())
        {
            CombatParry.Notify(owner, player); // 소유권 변경 전 발사자의 패링 누적 규칙을 처리합니다.
            // 발사자 소유권이 플레이어로 바뀌기 전에 해당 연습 허수아비의 성공만 기록합니다.
            var dummy = owner != null ? owner.GetComponent<TutorialTrainingDummy>() : null;
            if (dummy != null) dummy.RecordParry(player);
            // 실제 투사체를 돌려보냅니다. 발사자를 새 목표로, 패링한 플레이어를 새 소유자로 설정합니다.
            Vector2 returnDirection = owner != null
                ? (Vector2)owner.transform.position - closestHit.centroid : -direction;
            direction = returnDirection.sqrMagnitude > 0.0001f ? returnDirection.normalized : -direction;
            owner = player.gameObject;
            wasParried = true;
            if (dummy != null)
            {
                dummy.ShowReturnedProjectile(this);
                // 연습 탄환은 수명이 거의 끝난 순간 패링해도 발사자에게 돌아갈 시간을 확보합니다.
                remainingLifetime = Mathf.Max(remainingLifetime, returnDirection.magnitude / Mathf.Max(.1f, speed) + .25f);
            }
            transform.position = closestHit.centroid + direction * (radius + 0.02f);
            return;
        }

        // 패링한 탄환도 오류 규칙의 예외가 아닙니다. 반사 오류가 있는 적은 새 소유자인
        // 플레이어에게 피해를 되돌립니다. 이후 반사 피해의 재반사 방지는 적의 피해 처리에서 담당합니다.
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

    private bool TryFindClosestHit(Vector2 start, float moveDistance, out RaycastHit2D closestHit)
    {
        RaycastHit2D[] hits = Physics2D.CircleCastAll(start, radius, direction, moveDistance);
        closestHit = default;
        bool foundHit = false;

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null
                || hit.collider.GetComponentInParent<ReflectProjectile>() != null
                || IsOwnerCollider(hit.collider)
                || IsDetectionTrigger(hit.collider))
            {
                continue;
            }

            if (!foundHit || hit.distance < closestHit.distance)
            {
                closestHit = hit;
                foundHit = true;
            }
        }
        return foundHit;
    }

    // 입장/튜토리얼 감지 영역은 벽이 아닙니다. 생성 위치를 감싸는 Trigger에
    // CircleCast가 거리 0으로 맞아 탄환이 즉시 사라지지 않도록 제외합니다.
    // 단, 허수아비 등의 피격 Trigger 및 오류를 붙이는 대상의 Trigger는 유지합니다.
    private static bool IsDetectionTrigger(Collider2D collider)
    {
        return collider.isTrigger
            && !CombatDamageUtility.TryFindReceiver(collider.gameObject, out _)
            && collider.GetComponentInParent<PasteTarget>() == null;
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
