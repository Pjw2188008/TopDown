using UnityEngine;

/// <summary>
/// 접근 → 정지 → 공격 애니메이션 → 투사체 발사를 담당하는 일반 원거리 적입니다.
/// Inspector 설정과 실행 순서를 담당합니다. Internal의 partial 파일은 별도 부착하지 않습니다.
/// RangedEnemy 프리팹 루트에 부착합니다. EnemyController/MeleeEnemy/MovingEnemy와 중복 부착하지 않습니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer), typeof(Animator), typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D), typeof(EnemyStagger))]
public sealed partial class RangedEnemy : MonoBehaviour, ICombatDamageable, IAccelerationTarget
{
    [Header("추적 / 이동")]
    [Tooltip("추적할 플레이어입니다. 스포너가 지정하며, 비어 있으면 PlayerMove를 주기적으로 검색합니다.")]
    [SerializeField] private Transform target;
    [Tooltip("초당 이동 거리입니다. 가속 오류를 적용받으면 배율만큼 빨라집니다.")]
    [SerializeField, Min(0f)] private float moveSpeed = 1.8f;
    [Tooltip("이 거리 안에서는 접근을 멈춥니다. 공격 범위보다 작거나 같게 사용하는 것이 좋습니다.")]
    [SerializeField, Min(0f)] private float stoppingDistance = 3f;
    [Tooltip("새 공격을 시작할 수 있는 거리입니다. 시야/벽 차폐 검사는 하지 않습니다.")]
    [SerializeField, Min(.1f)] private float attackRange = 5f;
    [Tooltip("이동을 막는 레이어입니다. Trigger와 플레이어 몸 Collider는 이동을 막지 않습니다. 길찾기는 하지 않습니다.")]
    [SerializeField] private LayerMask movementBlockingLayers = Physics2D.DefaultRaycastLayers;

    [Header("공격 타이밍 / 애니메이션")]
    [Tooltip("공격 시작부터 다음 공격 시작까지 최소 간격 N초입니다. 애니메이션이 더 길면 끝난 뒤 다음 공격을 시작합니다.")]
    [SerializeField, Min(.1f)] private float attackInterval = 1.5f;
    [Tooltip("생성 후 첫 공격까지 대기하는 시간입니다.")]
    [SerializeField, Min(0f)] private float firstAttackDelay = 1f;
    [Tooltip("공격 클립 진행률 0~1 중 투사체가 발사될 시점입니다. 예: 0.65는 65% 지점. Animation Event는 필요 없습니다.")]
    [SerializeField, Range(0f, 1f)] private float releaseNormalizedTime = .65f;
    [Tooltip("Animator에 Ranged_Attack 클립이 없거나 길이가 0이면 사용하는 공격 시간입니다.")]
    [SerializeField, Min(.05f)] private float fallbackAttackDuration = .5f;

    [Header("투사체")]
    [Tooltip("투사체 스프라이트입니다. 비어 있으면 임시 사각형을 표시합니다.")]
    // 투사체 설정은 애니메이션 프레임이 아닙니다. Sprite 드롭 시 SpriteRenderer와
    // 이 필드 사이의 대상 선택 메뉴가 생기지 않도록 키프레임 대상에서 제외합니다.
    [SerializeField, UnityEngine.Animations.NotKeyable] private Sprite projectileSprite;
    [Tooltip("투사체의 초당 이동 속도입니다. 발사 순간의 플레이어 위치를 향해 직진하며, 발사 후에는 추적하지 않습니다.")]
    [SerializeField, Min(.1f)] private float projectileSpeed = 10f;
    [Tooltip("투사체가 주는 피해량입니다. 플레이어의 가드/패링 및 반사 처리를 사용합니다.")]
    [SerializeField, Min(.1f)] private float projectileDamage = 1f;
    [Tooltip("투사체의 최대 생존 시간입니다.")]
    [SerializeField, Min(.1f)] private float projectileLifetime = 6f;
    [Tooltip("투사체의 월드 충돌 반지름입니다. 그림도 지름에 맞춥니다.")]
    [SerializeField, Min(.01f)] private float projectileRadius = .12f;
    [Tooltip("몬스터 중심에서 공격 방향으로 떨어진 발사 위치입니다. 몸 크기에 맞춰 조절하세요.")]
    [SerializeField, Min(0f)] private float muzzleDistance = .45f;
    [Tooltip("반사 표면에 부딪힐 때 허용하는 최대 반사 횟수입니다. 일반 벽에는 반사하지 않습니다.")]
    [SerializeField, Min(0)] private int maxBounces = 2;
    [Tooltip("투사체 색입니다.")]
    [SerializeField] private Color projectileColor = new Color(1f, .75f, .15f);

    [Header("체력")]
    [Tooltip("최대 체력입니다. 0이 되면 몸 Collider를 끄고 제거합니다.")]
    [SerializeField, Min(1f)] private float maxHealth = 5f;

    private SpriteRenderer visual;
    private Animator animator;
    private Rigidbody2D body;
    private EnemyStagger stagger;
    private BoxCollider2D bodyCollider;
    private float health;
    private bool dead;

    public float CurrentHealth => health;
    public bool IsAttacking => attacking;
    public float CurrentMoveSpeed => Mathf.Max(0f, moveSpeed) * speedMultiplier;

    private void Awake()
    {
        visual = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
        stagger = GetComponent<EnemyStagger>();
        health = Mathf.Max(1f, maxHealth);
        ConfigureBody();

        int layer = LayerMask.NameToLayer("Enemy");
        if (layer >= 0) gameObject.layer = layer;
    }

    private void OnEnable()
    {
        nextAttackAt = Time.time + Mathf.Max(0f, firstAttackDelay);
    }

    public void SetTarget(Transform player)
    {
        target = player;
        RefreshPlayerCollisions();
    }

    public void SetAccelerationMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Max(1f, multiplier);
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    private void Tick(float deltaTime)
    {
        if (dead || deltaTime <= 0f) return;

        FindTarget();
        RefreshPlayerCollisions();
        if (stagger.IsStunned || target == null || !target.gameObject.activeInHierarchy)
        {
            CancelAttack();
            SetMoving(false);
            return;
        }

        if (attacking)
        {
            AdvanceAttack(deltaTime);
            return;
        }

        Vector2 difference = target.position - transform.position;
        float distance = difference.magnitude;
        Face(difference);
        if (distance <= Mathf.Max(.1f, attackRange) && Time.time >= nextAttackAt)
        {
            BeginAttack(difference);
            return;
        }

        MoveTowardsTarget(difference, distance, deltaTime);
    }

    public bool ReceiveDamage(float amount, GameObject source, bool canReflect)
    {
        if (dead || amount <= 0f) return false;

        float healthBeforeHit = health;
        health = Mathf.Max(0f, health - amount);
        if (health < healthBeforeHit) DamageBlink.Play(gameObject);
        if (health <= 0f)
        {
            dead = true;
            CancelAttack();
            SetMoving(false);
            bodyCollider.enabled = false;
            RestorePlayerCollisions();
            Destroy(gameObject);
        }
        return true;
    }

    private void OnDisable()
    {
        CancelAttack();
        SetMoving(false);
        RestorePlayerCollisions();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(.1f, attackRange));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, Mathf.Min(stoppingDistance, attackRange));
    }
}
