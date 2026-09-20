using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 루트에 부착하는 순찰/추적 근접 몬스터입니다. 예고/타격 이펙트, 체력,
/// 패링 경직, 플레이어 몸 충돌 제외를 담당합니다. MeleeEnemy/RangedEnemy와 중복 부착하지 마세요.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyStagger))]
public partial class EnemyController : MonoBehaviour, ICombatDamageable
{
    // 기존 공개 필드 이름은 유지하여 프리팹/Inspector 설정을 보존합니다.
    [Header("Stat Settings")]
    [Tooltip("적의 최대 체력입니다.")] public float maxHealth = 100f;
    [Tooltip("순찰/추적 이동 속도입니다.")] public float moveSpeed = 3f;
    [Tooltip("근접 타격 피해량입니다. 가드/패링은 PlayerMove에서 처리합니다.")] public float attackDamage = 10f;
    [Tooltip("타격 후 다음 공격 준비까지의 시간(초)입니다.")] public float attackCooldown = 1.5f;
    [Header("Range Settings (Box)")]
    [Tooltip("순찰 영역과 플레이어 감지 범위 크기입니다.")] public Vector2 patrolAreaSize = new Vector2(10f, 10f);
    [Tooltip("공격 준비를 시작하는 중심 사각 범위입니다. 실제 타격 범위는 Attack Hit Size입니다.")]
    public Vector2 attackAreaSize = new Vector2(2f, 2f);
    [Header("근접 공격 판정 / 이펙트")]
    [Tooltip("주황색 공격 예고 시간입니다. 끝나는 순간 우클릭하면 기존 패링 판정이 적용됩니다.")]
    [Min(.01f)] public float attackWindup = .4f;
    [Tooltip("몬스터 중심에서 공격 방향으로 떨어진 타격 중심까지 거리입니다.")]
    [Min(0f)] public float attackReach = .75f;
    [Tooltip("오른쪽 공격 기준 타격 사각형 크기입니다. 위/아래 공격 시 함께 회전합니다.")]
    public Vector2 attackHitSize = new Vector2(1.2f, 1.2f);
    [Tooltip("오른쪽을 향하는 공격 이미지입니다. 비우면 플레이어 이펙트, 그것도 없으면 임시 빨간 사각형을 사용합니다.")]
    public Sprite attackEffectSprite;
    [Tooltip("타격 이펙트 유지 시간입니다. 이펙트가 남아 있어도 타격은 한 번만 발생합니다.")]
    [Min(.01f)] public float attackEffectDuration = .18f;
    [Tooltip("실제 타격 영역에 주황색 사각 예고를 표시합니다.")] public bool showAttackWarning = true;
    [Header("References / 충돌")]
    [Tooltip("플레이어 Transform입니다. 비우면 활성 PlayerMove를 찾습니다.")] public Transform player;
    [Tooltip("이 적과 플레이어 몸 Collider만 충돌하지 않게 합니다. 공격 검색/투사체 판정은 유지합니다.")]
    public bool ignorePlayerBodyCollision = true;
    [Tooltip("Default인 이 적의 Collider를 실행 중 Enemy 레이어로 지정합니다. 사용자 지정 레이어는 유지합니다.")]
    public bool autoAssignEnemyLayer = true;

    private float currentHealth, attackTimer;
    private Vector2 patrolCenter, targetPatrolPoint;
    private enum State { Patrol, Chase, Attack }
    private State currentState;
    private PlayerMove target, collisionTarget;
    private EnemyStagger stagger;
    private SpriteRenderer bodyRenderer;
    private bool windingUp, dead;
    private float windupRemaining, effectRemaining;
    private Vector2 swingDirection = Vector2.right;
    private Vector2 swingCenter;
    private GameObject effectObject;
    private SpriteRenderer effectRenderer;
    private Texture2D fallbackTexture;
    private Sprite fallbackSprite;
    private struct CollisionPair { public Collider2D enemy, player; }
    private readonly List<CollisionPair> ignoredPairs = new List<CollisionPair>();
    private readonly List<Collider2D> ownColliderBuffer = new List<Collider2D>(4);
    private readonly List<Collider2D> playerColliderBuffer = new List<Collider2D>(4);
    public float CurrentHealth => currentHealth;

    private void Awake()
    {
        currentHealth = Mathf.Max(1f, maxHealth);
        stagger = GetComponent<EnemyStagger>();
        if (stagger == null) stagger = gameObject.AddComponent<EnemyStagger>();
        if (GetComponentInChildren<Collider2D>() == null) gameObject.AddComponent<BoxCollider2D>();
        bodyRenderer = GetComponentInChildren<SpriteRenderer>();
        ResolvePlayer(); RefreshBodyCollisions();
    }
    private void Start()
    {
        patrolCenter = transform.position; SetNewPatrolPoint();
        ResolvePlayer(); RefreshBodyCollisions();
    }
    private void Update()
    {
        if (dead || currentHealth <= 0f) return;
        ResolvePlayer(); RefreshBodyCollisions();
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        attackTimer = Mathf.Max(0f, attackTimer - dt); UpdateEffect(dt);
        if (stagger.IsStunned) { CancelAttack(); attackTimer = Mathf.Max(.01f, attackCooldown); return; }
        if (target == null || !target.isActiveAndEnabled) { CancelAttack(); return; }
        if (windingUp)
        {
            windupRemaining -= dt;
            if (windupRemaining <= 0f) ResolveAttack();
            return;
        }
        switch (currentState)
        {
            case State.Patrol: PatrolLogic(); break;
            case State.Chase: ChaseLogic(); break;
            case State.Attack: AttackLogic(); break;
        }
    }
    private Vector2 HitSize => new Vector2(Mathf.Max(.01f, attackHitSize.x), Mathf.Max(.01f, attackHitSize.y));
    private float SwingAngle => Mathf.Atan2(swingDirection.y, swingDirection.x) * Mathf.Rad2Deg;
    private void OnDisable() { CancelAttack(); RestoreBodyCollisions(); }
    private void OnDestroy()
    {
        RestoreBodyCollisions();
        if (effectObject != null) Destroy(effectObject);
        if (fallbackSprite != null) Destroy(fallbackSprite);
        if (fallbackTexture != null) Destroy(fallbackTexture);
    }
}
