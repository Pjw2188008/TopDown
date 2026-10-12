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
    // 발사자가 죽거나 파괴되어도 진영은 유지합니다. 표면 반사는 진영을 바꾸지 않습니다.
    private bool playerOwned;
    public Vector2 TravelDirection => direction;
    /// <summary>오류 배율까지 반영된 실제 원형 판정 반지름. 충격파 그림 크기 동기화에 사용합니다.</summary>
    public float HitRadius => radius;
    /// <summary>충격파처럼 귀환을 끝까지 보여 줄 투사체만 켭니다. 일반 탄환은 기존 수명을 유지합니다.</summary>
    public bool PreserveParryReturnLifetime { get; set; }
    /// <summary>사이드뷰 보스 충격파만 사용합니다. 패링/표면 반사 후에도 수평을 유지합니다.</summary>
    public bool HorizontalOnly { get; set; }
    public event System.Action<PlayerMove> Parried;
    /// <summary>발사 회차가 필요한 보스만 지정합니다. 미지정 시 기존 공격자 패링 알림을 사용합니다.</summary>
    public System.Action<PlayerMove> ParryReceiver { get; set; }
    private float patternSize = 1f, patternSpeed = 1f;
    private bool patternWallReflection;

    /// <summary>비행 중 오류만 변경합니다. 방향/소유권/패링 상태와 남은 비행 거리는 보존합니다.</summary>
    public void SetPatternModifiers(float sizeMultiplier, float speedMultiplier, bool reflectWalls, int bounces)
    {
        float nextSize = Mathf.Max(.01f, sizeMultiplier), nextSpeed = Mathf.Max(.01f, speedMultiplier);
        radius *= nextSize / patternSize;
        transform.localScale *= nextSize / patternSize;
        speed *= nextSpeed / patternSpeed;
        remainingLifetime *= patternSpeed / nextSpeed;
        if (reflectWalls && !patternWallReflection) remainingBounces = Mathf.Max(1, bounces);
        patternSize = nextSize; patternSpeed = nextSpeed; patternWallReflection = reflectWalls;
    }

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
        playerOwned = owner != null && owner.GetComponentInParent<PlayerMove>() != null;
        patternSize = patternSpeed = 1f;
        patternWallReflection = false;
        ParryReceiver = null;
        wasParried = false;
        HorizontalOnly = false;
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
            GameObject originalOwner = owner;
            // 발사자 소유권이 플레이어로 바뀌기 전에 해당 연습 허수아비의 성공만 기록합니다.
            var dummy = owner != null ? owner.GetComponent<TutorialTrainingDummy>() : null;
            if (dummy != null) dummy.RecordParry(player);
            // 실제 투사체를 돌려보냅니다. 발사자를 새 목표로, 패링한 플레이어를 새 소유자로 설정합니다.
            Vector2 returnDirection = owner != null
                ? (Vector2)owner.transform.position - closestHit.centroid : -direction;
            if (HorizontalOnly)
                returnDirection = Mathf.Abs(returnDirection.x) > .0001f ? new Vector2(returnDirection.x,0f) : -direction;
            direction = returnDirection.sqrMagnitude > 0.0001f ? returnDirection.normalized : -direction;
            owner = player.gameObject;
            playerOwned = true;
            wasParried = true;
            if (PreserveParryReturnLifetime)
                remainingLifetime = Mathf.Max(remainingLifetime, returnDirection.magnitude / Mathf.Max(.1f, speed) + .25f);
            if (dummy != null)
            {
                dummy.ShowReturnedProjectile(this);
                // 연습 탄환은 수명이 거의 끝난 순간 패링해도 발사자에게 돌아갈 시간을 확보합니다.
                remainingLifetime = Mathf.Max(remainingLifetime, returnDirection.magnitude / Mathf.Max(.1f, speed) + .25f);
            }
            transform.position = closestHit.centroid + direction * (radius + 0.02f);
            // 먼저 반환 상태로 바꿔야 N번째 패링 경직에서도 이 탄환을 적대 탄환과 구별할 수 있습니다.
            if (ParryReceiver != null) ParryReceiver(player);
            else CombatParry.Notify(originalOwner, player);
            Parried?.Invoke(player);
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

        direction = HorizontalOnly ? -direction : Vector2.Reflect(direction, normal).normalized;
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
                || IsFriendlyCombatant(hit.collider)
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

    // 현재 공통 피해 수신자는 플레이어와 적/보스/훈련용 적입니다.
    // 적 탄환은 다른 적을 충돌 단계부터 제외해야 피해 없이 사라지거나 방패가 되는 일도 없습니다.
    // 벽·상자·반사 표면처럼 피해 수신자가 없는 환경은 기존 충돌 규칙을 유지합니다.
    private bool IsFriendlyCombatant(Collider2D collider)
    {
        if (playerOwned) return false;
        return CombatDamageUtility.TryFindReceiver(collider.gameObject, out var receiver)
            && !(receiver is PlayerMove);
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
        if (patternWallReflection && !hitCollider.isTrigger) return true;
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
