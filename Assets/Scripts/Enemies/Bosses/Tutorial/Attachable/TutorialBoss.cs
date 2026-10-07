using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>방 안의 플레이어를 추적하고 거리별 근접 휘두르기/투사체를 사용합니다. 공격과 N회 패링 경직 중에는 멈춥니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
public sealed partial class TutorialBoss : MonoBehaviour, ICombatDamageable, ICombatParryReceiver
{
    [Header("대상 / 체력")]
    [Tooltip("비우면 활성 PlayerMove를 자동 검색합니다.")]
    [SerializeField] private PlayerMove player;
    [SerializeField, Min(1f), Tooltip("보스 최대 체력입니다.")] private float maxHealth = 40f;
    [Header("방 입장 / 추적")]
    [SerializeField, Tooltip("보스 방의 BoxCollider2D를 연결하세요(Is Trigger 권장). 보스 자신의 콜라이더가 아닌 고정된 방 오브젝트를 사용합니다. 방 밖에서는 추적/새 공격을 멈춥니다.")] private BoxCollider2D roomArea;
    [SerializeField, Tooltip("Room Area 미지정 시 보스 최초 위치 기준 방의 중심 오프셋입니다. 보스가 움직여도 범위는 따라가지 않습니다.")] private Vector2 roomOffset;
    [SerializeField, Tooltip("Room Area 미지정 시 사각형 방 범위의 가로/세로 크기입니다. 씬 기즈모로 표시됩니다.")] private Vector2 roomSize = new Vector2(24f,16f);
    [SerializeField, Min(0f), Tooltip("공격 사이 대기 중 플레이어를 추적하는 초당 이동 거리입니다.")] private float chaseSpeed = 2f;
    [SerializeField, Min(0f), Tooltip("플레이어에게 접근 후 멈출 중심 간 거리입니다. 근접 범위의 90% 이하로 제한합니다.")] private float stoppingDistance = 1.8f;
    [SerializeField, Min(0f), Tooltip("다음 공격까지 쉬는 시간입니다.")] private float attackCooldown = 1.2f;
    [SerializeField, Tooltip("공격 시야를 막는 벽 레이어입니다. Trigger는 제외합니다.")] private LayerMask blockingLayers = Physics2D.DefaultRaycastLayers;
    [Header("근접 휘두르기")]
    [SerializeField, Min(.1f), Tooltip("이 거리 이내면 근접 휘두르기, 바깥이면 원거리 패턴을 선택합니다.")] private float meleeRange = 2.5f;
    [SerializeField, InspectorName("Slam Windup Time"), Min(.1f), Tooltip("근접 애니메이션 시작부터 Slam Hit Frame의 실제 타격까지 걸리는 시간입니다. 클립 재생 속도를 자동 조절하며 후딜 프레임도 끝까지 재생합니다.")] private float slamWarningTime = .9f;
    [SerializeField, InspectorName("Melee Hit Offset"), Tooltip("오른쪽을 바라볼 때 보스 중심 기준 공격 사각형 위치(X/Y, 월드 단위)입니다. 왼쪽을 바라보면 X만 자동 반전합니다. Y는 위/아래 위치이며 음수도 가능합니다.")] private Vector2 meleeHitBoxOffset = new Vector2(1.4f,0f);
    [SerializeField, InspectorName("Melee Hit Size"), Tooltip("실제 공격 사각형의 가로(X)/세로(Y) 크기입니다. 월드 단위이며 청록색 기즈모와 판정이 일치합니다. 보스 몸 콜라이더와 별개입니다.")] private Vector2 meleeHitBoxSize = new Vector2(2.2f,2.2f);
    [SerializeField, Min(.1f)] private float slamDamage = 2f;
    [SerializeField, Min(.01f), Tooltip("이 보스의 근접 공격에만 적용하는 최소 패링 허용 시간입니다. 기본 0.3초: 타격 전 0.3초 안에 우클릭을 시작하고 유지하면 성공합니다. 일반 적/투사체의 패링 시간은 바꾸지 않으며 플레이어 기본값이 더 길면 그 값을 유지합니다.")] private float meleeParryWindow = .3f;
    [Header("원거리 공격")]
    [SerializeField, InspectorName("Shot Windup Time"), Min(.1f), Tooltip("방향을 고정한 뒤 투사체 발사까지 준비하는 시간입니다. 조준 경고선은 표시하지 않으며 발사 후 유도하지 않습니다.")] private float shotWarningTime = 1f;
    [SerializeField, Min(.1f)] private float projectileSpeed = 7f;
    [SerializeField, Min(.1f)] private float projectileDamage = 1f;
    [SerializeField, Min(.03f), Tooltip("탄환 그림과 충돌 반지름입니다.")] private float projectileRadius = .18f;
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
    private Vector2 roomOrigin;
    private Rigidbody2D movementBody;
    private readonly List<RaycastHit2D> movementHits = new List<RaycastHit2D>();
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
        movementBody = body;roomOrigin = transform.position;sight.useTriggerBody = true;
        body.gravityScale = 0;body.constraints = RigidbodyConstraints2D.FreezeRotation;
        GetComponent<BoxCollider2D>().isTrigger = true; // 몸끼리 밀지 않으며 공격 검색에서는 피격 대상으로 사용합니다.
        int layer = LayerMask.NameToLayer("Enemy");if (layer >= 0 && gameObject.layer == 0) gameObject.layer = layer;
        PrepareVisuals();
        PrepareAnimation();
        SetPhase(Phase.Recovery, attackCooldown);
    }

    private void Update() => Tick(Time.deltaTime);
    private void Tick(float dt)
    {
        if (dt <= 0f) return;
        Vector2 before = transform.position;
        TickCombat(dt);
        UpdateMovementAnimation((Vector2)transform.position-before);
    }

    private void TickCombat(float dt)
    {
        if (dt <= 0 || phase == Phase.Dead) return;
        if (player == null && Time.time >= nextSearch)
        { player = FindFirstObjectByType<PlayerMove>();nextSearch = Time.time + .5f; }
        if (player == null || !player.isActiveAndEnabled)
        {
            if (!IsStaggered) SetPhase(Phase.Ready, 0f);
            else { phaseRemaining -= dt;if (phaseRemaining <= 0) SetPhase(Phase.Recovery, attackCooldown); }
            return;
        }
        if (!IsPlayerInRoom())
        {
            // 방을 나가거나 리스폰한 대상에게 진행 중 공격을 계속 보내지 않습니다.
            StopAttackAnimation();
            if (!IsStaggered) SetPhase(Phase.Recovery, attackCooldown);
            else { phaseRemaining -= dt;if (phaseRemaining <= 0f) SetPhase(Phase.Recovery, attackCooldown); }
            return;
        }
        if (attackAnimationActive) return; // Animator 평가 후 LateUpdate에서 타격과 후딜을 처리합니다.
        if (phase == Phase.Ready || phase == Phase.Recovery) ChasePlayer(dt);
        if (phase == Phase.Ready)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (!CanSee(player.transform.position)) return;
            lockedPoint = player.transform.position;
            Vector2 aim = lockedPoint - (Vector2)transform.position;
            shotDirection = aim.sqrMagnitude > .0001f ? aim.normalized : Vector2.right;
            FaceAttackDirection(shotDirection);
            SetPhase(distance <= Mathf.Max(.1f, meleeRange) ? Phase.SlamWarning : Phase.ShotWarning,
                distance <= Mathf.Max(.1f, meleeRange) ? slamWarningTime : shotWarningTime);
            BeginBossAttackAnimation();return;
        }
        phaseRemaining -= dt;
        if (phaseRemaining > 0) return;
        Phase completed = phase;
        // 먼저 후딜로 전환하여 피해 콜백의 N번째 패링 경직을 덮어쓰지 않습니다.
        SetPhase(Phase.Recovery, attackCooldown);
        if (completed == Phase.SlamWarning) Slam();
        else if (completed == Phase.ShotWarning) Fire();
        else if (completed == Phase.Recovery) SetPhase(Phase.Ready, 0);
    }

    private bool IsInsideRoom(Vector2 point)
    {
        if (roomArea != null)
            return roomArea.enabled && roomArea.gameObject.activeInHierarchy
                && !roomArea.transform.IsChildOf(transform) && roomArea.OverlapPoint(point);
        Vector2 center = (Application.isPlaying ? roomOrigin : (Vector2)transform.position) + roomOffset;
        Vector2 half = new Vector2(Mathf.Max(.1f,roomSize.x),Mathf.Max(.1f,roomSize.y)) * .5f;
        return Mathf.Abs(point.x-center.x) <= half.x && Mathf.Abs(point.y-center.y) <= half.y;
    }

    private bool IsPlayerInRoom() => player != null && player.isActiveAndEnabled && IsInsideRoom(player.transform.position);

    private void ChasePlayer(float dt)
    {
        float distance = Vector2.Distance(transform.position,player.transform.position);
        float stop = CanSee(player.transform.position) ? Mathf.Min(Mathf.Max(0f,stoppingDistance),Mathf.Max(.1f,meleeRange)*.9f) : 0f;
        float wanted = Mathf.Min(Mathf.Max(0f,chaseSpeed)*dt,Mathf.Max(0f,distance-stop));
        Vector2 step = sight.Step(transform,player.transform,player.transform.position,wanted,blockingLayers);
        if (step.sqrMagnitude < .00000001f) return;
        // Trigger인 보스 피격 콜라이더도 벽 검사에 사용하지만 플레이어 몸과는 밀지 않습니다.
        float allowed = InteractionMotion.AllowedDistance(transform,player.transform,step.normalized,step.magnitude,
            blockingLayers,movementHits,default,true);
        Vector2 next = (Vector2)transform.position + step.normalized*allowed;
        if (!IsInsideRoom(next)) return;
        movementBody.position = next;
        transform.position = new Vector3(next.x,next.y,transform.position.z);
        Physics2D.SyncTransforms();
    }

    private bool CanSee(Vector2 point) => sight.HasSight(transform, player != null ? player.transform : null, point, blockingLayers);
    private void SetPhase(Phase value, float duration)
    {
        phase = value;phaseDuration = phaseRemaining = Mathf.Max(0f, duration);AnimatePhase(value);
    }
    private void Slam()
    {
        if (!IsPlayerInRoom()) return;
        // 실제 애니메이션 타격 프레임에 보스 앞쪽의 피해/패링만 처리합니다.
        ResolveSlam();
    }

    private void ResolveSlam()
    {
        if (player == null || !player.isActiveAndEnabled || !CanSee(player.transform.position)) return;
        if (IsPlayerInSlamArea()) player.ReceiveMeleeAttack(Mathf.Max(.1f, slamDamage), gameObject, Mathf.Max(.01f,meleeParryWindow));
    }

    private bool IsPlayerInSlamArea()
    {
        Physics2D.SyncTransforms();
        foreach (var hit in Physics2D.OverlapBoxAll(MeleeHitCenter, MeleeHitSize, 0f))
            if (hit.enabled && !hit.isTrigger && hit.GetComponentInParent<PlayerMove>() == player)
                return true;
        return false;
    }

    // 실제 그림의 좌우 반전 상태를 사용하여 편집/재생 모드의 기즈모와 타격이 같은 쪽을 향합니다.
    // 사각형은 회전하지 않으며, 위/아래 위치는 사용자가 지정한 Y 오프셋을 유지합니다.
    private Vector2 MeleeHitCenter => (Vector2)transform.position
        + new Vector2(meleeHitBoxOffset.x * MeleeFacingSign, meleeHitBoxOffset.y);
    private Vector2 MeleeHitSize => new Vector2(Mathf.Max(.01f,meleeHitBoxSize.x),Mathf.Max(.01f,meleeHitBoxSize.y));
    private float MeleeFacingSign
    {
        get
        {
            var visual = bodyVisual != null ? bodyVisual : GetComponent<SpriteRenderer>();
            bool flipped = visual != null && visual.flipX;
            bool negativeScale = transform.lossyScale.x < 0f;
            return (moveSpriteFacesRight ^ flipped ^ negativeScale) ? 1f : -1f;
        }
    }

    private void Fire()
    {
        if (!IsPlayerInRoom()) return;
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
        SetPhase(Phase.Staggered, Mathf.Max(.1f, staggerDuration));onStaggered.Invoke();
    }

    public bool ReceiveDamage(float amount, GameObject source, bool canReflect)
    {
        if (!isActiveAndEnabled || phase == Phase.Dead || amount <= 0) return false;
        health = Mathf.Max(0, health - amount);DamageBlink.Play(gameObject);
        if (health <= 0)
        {
            SetPhase(Phase.Dead,0);GetComponent<BoxCollider2D>().enabled = false;
            onDefeated.Invoke();Destroy(gameObject);
        }
        return true;
    }

    private void OnDisable()
    {
        StopAttackAnimation();
        foreach (var shot in projectiles) if (shot != null) Destroy(shot.gameObject);
        projectiles.Clear();
        if (phase != Phase.Dead) SetPhase(Phase.Recovery, attackCooldown);
    }
    private void OnDestroy()
    {
        if (runtimeCircle != null) Destroy(runtimeCircle);if (runtimeSquare != null) Destroy(runtimeSquare);
        if (runtimeCircleTexture != null) Destroy(runtimeCircleTexture);if (runtimeSquareTexture != null) Destroy(runtimeSquareTexture);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;Gizmos.DrawWireSphere(transform.position, Mathf.Max(.1f, meleeRange));
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(MeleeHitCenter,MeleeHitSize);
        Gizmos.DrawLine(transform.position,MeleeHitCenter);
        Gizmos.color = new Color(.3f,.6f,1f);
        if (roomArea != null)
        {
            Matrix4x4 previous = Gizmos.matrix;Gizmos.matrix = roomArea.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(roomArea.offset,roomArea.size);Gizmos.matrix = previous;
        }
        else Gizmos.DrawWireCube((Application.isPlaying ? roomOrigin : (Vector2)transform.position)+roomOffset,
            new Vector3(Mathf.Max(.1f,roomSize.x),Mathf.Max(.1f,roomSize.y),0f));
    }
}
