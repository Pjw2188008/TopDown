using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>튜토리얼 보스 루트에 부착합니다. 고정 위치에서 내리찍기/투사체를 사용하고 N회 패링에 경직됩니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
public sealed partial class TutorialBoss : MonoBehaviour, ICombatDamageable, ICombatParryReceiver
{
    [Header("대상 / 체력")]
    [Tooltip("비우면 활성 PlayerMove를 자동 검색합니다.")]
    [SerializeField] private PlayerMove player;
    [SerializeField, Min(1f), Tooltip("보스 최대 체력입니다.")] private float maxHealth = 40f;
    [SerializeField, Min(.1f), Tooltip("이 거리 안에 플레이어가 있어야 패턴을 시작합니다.")] private float engagementRange = 12f;
    [SerializeField, Min(0f), Tooltip("다음 공격까지 쉬는 시간입니다.")] private float attackCooldown = 1.2f;
    [SerializeField, Tooltip("경고/공격 시야를 막는 벽 레이어입니다. Trigger는 제외합니다.")] private LayerMask blockingLayers = Physics2D.DefaultRaycastLayers;
    [Header("근접 내리찍기")]
    [SerializeField, Min(.1f), Tooltip("이 거리 이내면 내리찍기, 바깥이면 원거리 패턴을 선택합니다.")] private float meleeRange = 2.5f;
    [SerializeField, Min(.1f), Tooltip("플레이어 위치를 고정한 뒤 내리찍기까지 경고하는 시간입니다.")] private float slamWarningTime = .9f;
    [SerializeField, Min(.1f), Tooltip("원형 경고와 실제 타격의 반지름입니다.")] private float slamRadius = 1.1f;
    [SerializeField, Min(.1f)] private float slamDamage = 2f;
    [Header("원거리 공격")]
    [SerializeField, Min(.1f), Tooltip("고정된 조준선으로 발사 방향을 경고하는 시간입니다. 발사 후 유도하지 않습니다.")] private float shotWarningTime = 1f;
    [SerializeField, Min(.1f)] private float projectileSpeed = 7f;
    [SerializeField, Min(.1f)] private float projectileDamage = 1f;
    [SerializeField, Min(.03f), Tooltip("탄환 그림과 충돌 반지름입니다. 경고선의 폭은 탄환 지름과 같습니다.")] private float projectileRadius = .18f;
    [SerializeField, Min(.1f)] private float projectileLifetime = 5f;
    [Header("패링 누적 경직")]
    [SerializeField, Min(1), Tooltip("근접/투사체 패링을 합산합니다. N회 성공하면 경직되고 횟수가 초기화됩니다. 시간에 따른 자동 회복은 없습니다.")] private int parriesToStagger = 3;
    [SerializeField, Min(.1f), Tooltip("누적 패링 달성 시 공격하지 못하는 시간입니다.")] private float staggerDuration = 2.5f;
    [SerializeField] private UnityEvent onStaggered = new UnityEvent();
    [SerializeField] private UnityEvent onDefeated = new UnityEvent();

    private enum Phase { Ready, SlamWarning, ShotWarning, Recovery, Staggered, Dead }
    private Phase phase;
    private float phaseRemaining, phaseDuration, health, nextSearch;
    private int parryCount;
    private Vector2 lockedPoint, shotDirection;
    private readonly EnemyNavigation sight = new EnemyNavigation();
    private readonly List<ReflectProjectile> projectiles = new List<ReflectProjectile>();
    public float CurrentHealth => health;
    public int ParryCount => parryCount;
    public int ParriesToStagger => Mathf.Max(1, parriesToStagger);
    public bool IsStaggered => phase == Phase.Staggered;

    private void Awake()
    {
        health = Mathf.Max(1f, maxHealth);
        var body = GetComponent<Rigidbody2D>();body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0;body.constraints = RigidbodyConstraints2D.FreezeRotation;
        GetComponent<BoxCollider2D>().isTrigger = true; // 몸끼리 밀지 않으며 공격 검색에서는 피격 대상으로 사용합니다.
        int layer = LayerMask.NameToLayer("Enemy");if (layer >= 0 && gameObject.layer == 0) gameObject.layer = layer;
        PrepareVisuals();
        SetPhase(Phase.Recovery, attackCooldown);
    }

    private void Update() => Tick(Time.deltaTime);
    private void Tick(float dt)
    {
        if (dt <= 0 || phase == Phase.Dead) return;
        if (player == null && Time.time >= nextSearch)
        { player = FindFirstObjectByType<PlayerMove>();nextSearch = Time.time + .5f; }
        if (player == null || !player.isActiveAndEnabled)
        {
            if (!IsStaggered) SetPhase(Phase.Ready, 0f);
            else { phaseRemaining -= dt;if (phaseRemaining <= 0) SetPhase(Phase.Recovery, attackCooldown); }
            HideWarning();RestoreBodyPose();return;
        }
        if (phase == Phase.Ready)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance > Mathf.Max(meleeRange, engagementRange) || !CanSee(player.transform.position)) return;
            lockedPoint = player.transform.position;
            Vector2 aim = lockedPoint - (Vector2)transform.position;
            shotDirection = aim.sqrMagnitude > .0001f ? aim.normalized : Vector2.right;
            SetPhase(distance <= Mathf.Max(.1f, meleeRange) ? Phase.SlamWarning : Phase.ShotWarning,
                distance <= Mathf.Max(.1f, meleeRange) ? slamWarningTime : shotWarningTime);
            ShowWarning();return;
        }
        phaseRemaining -= dt;
        UpdateWarningAndPose();
        if (phaseRemaining > 0) return;
        Phase completed = phase;
        // 먼저 후딜로 전환하여 피해 콜백의 N번째 패링 경직을 덮어쓰지 않습니다.
        SetPhase(Phase.Recovery, attackCooldown);HideWarning();RestoreBodyPose();
        if (completed == Phase.SlamWarning) Slam();
        else if (completed == Phase.ShotWarning) Fire();
        else if (completed == Phase.Recovery) SetPhase(Phase.Ready, 0);
    }

    private bool CanSee(Vector2 point) => sight.HasSight(transform, player != null ? player.transform : null, point, blockingLayers);
    private void SetPhase(Phase value, float duration) { phase = value;phaseDuration = phaseRemaining = Mathf.Max(0f, duration); }
    private void Slam()
    {
        if (!CanSee(lockedPoint)) return;
        ShowImpact(lockedPoint);
        Physics2D.SyncTransforms();
        foreach (var hit in Physics2D.OverlapCircleAll(lockedPoint, Mathf.Max(.1f, slamRadius)))
            if (hit.enabled && !hit.isTrigger && hit.GetComponentInParent<PlayerMove>() == player)
            { player.ReceiveMeleeAttack(Mathf.Max(.1f, slamDamage), gameObject);break; }
    }

    private void Fire()
    {
        var shot = new GameObject("Tutorial Boss Projectile");shot.transform.position = transform.position;
        var picture = new GameObject("Picture").AddComponent<SpriteRenderer>();picture.transform.SetParent(shot.transform,false);
        picture.sprite = projectileSprite != null ? projectileSprite : circleSprite;
        picture.color = new Color(1f,.35f,.1f);picture.sortingOrder = 30;
        Fit(picture, Vector2.one * Mathf.Max(.03f, projectileRadius) * 2);
        var shape = shot.AddComponent<CircleCollider2D>();shape.isTrigger = true;shape.radius = Mathf.Max(.03f, projectileRadius);
        var projectile = shot.AddComponent<ReflectProjectile>();
        projectile.Initialize(gameObject, shotDirection, projectileSpeed, projectileDamage, projectileRadius, projectileLifetime, 0, false);
        projectiles.RemoveAll(item => item == null);projectiles.Add(projectile);
    }

    public void ReceiveParry(PlayerMove parryingPlayer)
    {
        if (!isActiveAndEnabled || phase == Phase.Dead || IsStaggered || parryingPlayer == null) return;
        parryCount++;
        if (parryCount < ParriesToStagger) return;
        parryCount = 0;
        SetPhase(Phase.Staggered, Mathf.Max(.1f, staggerDuration));HideWarning();RestoreBodyPose();onStaggered.Invoke();
    }

    public bool ReceiveDamage(float amount, GameObject source, bool canReflect)
    {
        if (!isActiveAndEnabled || phase == Phase.Dead || amount <= 0) return false;
        health = Mathf.Max(0, health - amount);DamageBlink.Play(gameObject);
        if (health <= 0)
        {
            SetPhase(Phase.Dead,0);HideWarning();GetComponent<BoxCollider2D>().enabled = false;
            onDefeated.Invoke();Destroy(gameObject);
        }
        return true;
    }

    private void OnDisable()
    {
        HideWarning();RestoreBodyPose();
        if (impactVisual != null) impactVisual.gameObject.SetActive(false);
        foreach (var shot in projectiles) if (shot != null) Destroy(shot.gameObject);
        projectiles.Clear();
        if (phase != Phase.Dead) SetPhase(Phase.Recovery, attackCooldown);
    }
    private void OnDestroy()
    {
        if (warningVisual != null) Destroy(warningVisual.gameObject);if (impactVisual != null) Destroy(impactVisual.gameObject);
        if (runtimeCircle != null) Destroy(runtimeCircle);if (runtimeSquare != null) Destroy(runtimeSquare);
        if (runtimeCircleTexture != null) Destroy(runtimeCircleTexture);if (runtimeSquareTexture != null) Destroy(runtimeSquareTexture);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;Gizmos.DrawWireSphere(transform.position, Mathf.Max(.1f, meleeRange));
        Gizmos.color = Color.cyan;Gizmos.DrawWireSphere(transform.position, Mathf.Max(meleeRange, engagementRange));
    }
}
