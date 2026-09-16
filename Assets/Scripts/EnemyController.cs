using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 루트에 부착하는 순찰/추적 근접 몬스터입니다. 예고/타격 이펙트, 체력,
/// 패링 경직, 플레이어 몸 충돌 제외를 담당합니다. MeleeEnemy/ProjectileEnemy와 중복 부착하지 마세요.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyStagger))]
public class EnemyController : MonoBehaviour, ICombatDamageable
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
    private void ResolvePlayer()
    {
        target = player != null ? player.GetComponentInParent<PlayerMove>() : null;
        if (target == null) target = FindFirstObjectByType<PlayerMove>();
        if (target != null) player = target.transform;
    }
    private void SetNewPatrolPoint()
    {
        patrolCenter = transform.position; // 기존 순찰처럼 현재 위치에서 다음 목적지를 정합니다.
        targetPatrolPoint = patrolCenter + new Vector2(Random.Range(-Mathf.Abs(patrolAreaSize.x)/2f, Mathf.Abs(patrolAreaSize.x)/2f),
            Random.Range(-Mathf.Abs(patrolAreaSize.y)/2f, Mathf.Abs(patrolAreaSize.y)/2f));
    }
    private void MoveTowards(Vector2 point)
    {
        Vector2 next = Vector2.MoveTowards(transform.position, point, Mathf.Max(0f, moveSpeed) * Time.deltaTime);
        transform.position = new Vector3(next.x, next.y, transform.position.z);
    }
    private void PatrolLogic()
    {
        if (IsInsideBox(player.position, patrolAreaSize)) { currentState = State.Chase; return; }
        MoveTowards(targetPatrolPoint);
        if (Vector2.Distance(transform.position, targetPatrolPoint) < .1f) SetNewPatrolPoint();
    }
    private void ChaseLogic()
    {
        if (!IsInsideBox(player.position, patrolAreaSize)) { currentState = State.Patrol; SetNewPatrolPoint(); return; }
        if (IsInsideBox(player.position, attackAreaSize)) { currentState = State.Attack; return; }
        MoveTowards(player.position);
    }
    private void AttackLogic()
    {
        if (!IsInsideBox(player.position, attackAreaSize)) { currentState = State.Chase; return; }
        if (attackTimer <= 0f) BeginAttack();
    }
    private void BeginAttack()
    {
        if (target == null || dead || stagger.IsStunned) return;
        Vector2 delta = target.transform.position - transform.position;
        swingDirection = delta.sqrMagnitude < .0001f ? Vector2.right
            : Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) ? new Vector2(Mathf.Sign(delta.x), 0f)
            : new Vector2(0f, Mathf.Sign(delta.y));
        swingCenter = (Vector2)transform.position + swingDirection * Mathf.Max(0f, attackReach);
        windingUp = true; windupRemaining = Mathf.Max(.01f, attackWindup);
        effectRemaining = 0f;
        if (showAttackWarning) ShowEffect(true);
        else if (effectObject != null) effectObject.SetActive(false);
    }
    private Vector2 HitSize => new Vector2(Mathf.Max(.01f, attackHitSize.x), Mathf.Max(.01f, attackHitSize.y));
    private float SwingAngle => Mathf.Atan2(swingDirection.y, swingDirection.x) * Mathf.Rad2Deg;
    private void ResolveAttack()
    {
        if (!windingUp || dead || stagger.IsStunned || target == null || !target.isActiveAndEnabled) { CancelAttack(); return; }
        windingUp = false; // Consume before callbacks: one swing cannot deal repeated damage.
        attackTimer = Mathf.Max(.01f, attackCooldown); effectRemaining = Mathf.Max(.01f, attackEffectDuration);
        ShowEffect(false); Physics2D.SyncTransforms();
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(swingCenter, HitSize, SwingAngle))
        {
            if (!hit.enabled || hit.isTrigger || hit.GetComponentInParent<PlayerMove>() != target) continue;
            target.ReceiveMeleeAttack(Mathf.Max(0f, attackDamage), gameObject);
            break; // Multiple player colliders still count as one hit.
        }
    }
    private bool IsInsideBox(Vector3 position, Vector2 size)
    {
        Vector2 delta = position - transform.position;
        return Mathf.Abs(delta.x) <= Mathf.Abs(size.x)/2f && Mathf.Abs(delta.y) <= Mathf.Abs(size.y)/2f;
    }
    public void TakeDamage(float damage) => ReceiveDamage(damage, null, true);
    public bool ReceiveDamage(float amount, GameObject source, bool canReflect)
    {
        if (dead || !isActiveAndEnabled || amount <= 0f) return false;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        Debug.Log($"{name} 피해 {amount} / 남은 체력 {currentHealth}/{maxHealth}", this);
        if (currentHealth <= 0f) Die();
        return true;
    }
    private void Die()
    {
        dead = true; CancelAttack(); RestoreBodyCollisions();
        foreach (Collider2D shape in GetComponentsInChildren<Collider2D>()) shape.enabled = false;
        Destroy(gameObject);
    }
    private void RefreshBodyCollisions()
    {
        if (collisionTarget != target || !ignorePlayerBodyCollision) RestoreBodyCollisions();
        collisionTarget = target;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        foreach (Collider2D own in GetComponentsInChildren<Collider2D>())
        {
            if (autoAssignEnemyLayer && enemyLayer >= 0 && own.gameObject.layer == 0) own.gameObject.layer = enemyLayer;
            if (!ignorePlayerBodyCollision || target == null || !own.enabled || own.isTrigger) continue;
            foreach (Collider2D other in target.GetComponentsInChildren<Collider2D>())
            {
                if (!other.enabled || other.isTrigger || Physics2D.GetIgnoreCollision(own, other)) continue;
                if (!ignoredPairs.Exists(pair => pair.enemy == own && pair.player == other))
                    ignoredPairs.Add(new CollisionPair { enemy = own, player = other });
                Physics2D.IgnoreCollision(own, other, true);
            }
        }
    }
    private void RestoreBodyCollisions()
    {
        foreach (CollisionPair pair in ignoredPairs)
            if (pair.enemy != null && pair.player != null) Physics2D.IgnoreCollision(pair.enemy, pair.player, false);
        ignoredPairs.Clear(); collisionTarget = null;
    }
    private Sprite GetFallbackSprite()
    {
        if (fallbackSprite != null) return fallbackSprite;
        fallbackTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        fallbackTexture.SetPixel(0, 0, Color.white); fallbackTexture.Apply();
        fallbackSprite = Sprite.Create(fallbackTexture, new Rect(0,0,1,1), Vector2.one*.5f, 1f);
        return fallbackSprite;
    }
    private void ShowEffect(bool warning)
    {
        if (effectObject == null) { effectObject = new GameObject("EnemyMeleeAttackEffect"); effectRenderer = effectObject.AddComponent<SpriteRenderer>(); }
        Sprite sprite = warning ? GetFallbackSprite() : attackEffectSprite;
        if (sprite == null && target != null) sprite = target.AttackEffectVisual;
        if (sprite == null) sprite = GetFallbackSprite();
        effectRenderer.sprite = sprite;
        effectRenderer.color = warning ? new Color(1f,.65f,.1f,.25f) : new Color(1f,.35f,.25f,.8f);
        if (bodyRenderer != null) effectRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
        effectRenderer.sortingOrder = (bodyRenderer != null ? bodyRenderer.sortingOrder : 0) + 2;
        Vector2 size = sprite.bounds.size;
        Vector3 scale = new Vector3(HitSize.x / Mathf.Max(.0001f,size.x), HitSize.y / Mathf.Max(.0001f,size.y), 1f);
        Quaternion rotation = Quaternion.Euler(0,0,SwingAngle);
        effectObject.transform.localScale = scale; effectObject.transform.rotation = rotation;
        effectObject.transform.position = new Vector3(swingCenter.x,swingCenter.y,transform.position.z)
            - rotation * Vector3.Scale(sprite.bounds.center, scale);
        effectObject.SetActive(true);
    }
    private void UpdateEffect(float dt)
    {
        if (windingUp) return;
        effectRemaining = Mathf.Max(0f, effectRemaining-dt);
        if (effectRemaining <= 0f && effectObject != null) effectObject.SetActive(false);
    }
    private void CancelAttack()
    {
        windingUp = false; effectRemaining = 0f;
        if (effectObject != null) effectObject.SetActive(false);
    }
    private void OnDisable() { CancelAttack(); RestoreBodyCollisions(); }
    private void OnDestroy()
    {
        RestoreBodyCollisions();
        if (effectObject != null) Destroy(effectObject);
        if (fallbackSprite != null) Destroy(fallbackSprite);
        if (fallbackTexture != null) Destroy(fallbackTexture);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireCube(transform.position, (Vector3)patrolAreaSize);
        Gizmos.color = Color.red; Gizmos.DrawWireCube(transform.position, (Vector3)attackAreaSize);
        Matrix4x4 old = Gizmos.matrix;
        Vector2 center = Application.isPlaying && (windingUp || effectRemaining > 0f) ? swingCenter
            : (Vector2)transform.position + swingDirection * attackReach;
        Gizmos.matrix = Matrix4x4.TRS((Vector3)center, Quaternion.Euler(0,0,SwingAngle), Vector3.one);
        Gizmos.color = Color.cyan; Gizmos.DrawWireCube(Vector3.zero, (Vector3)HitSize); Gizmos.matrix = old;
    }
}
