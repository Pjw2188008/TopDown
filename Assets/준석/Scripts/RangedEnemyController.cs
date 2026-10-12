using UnityEngine;

/// <summary>준석 원거리 적의 순찰/단발·산탄 공격과 공통 플레이어 피해 연동을 담당합니다.</summary>
public class RangedEnemyController : MonoBehaviour, ICombatDamageable
{
    [Header("Stats Settings")]
    [Tooltip("적의 최대 체력")] public float maxHealth = 100f;
    [Tooltip("적의 이동 속도")] public float moveSpeed = 3f;

    [Header("Attack Settings")]
    [Tooltip("적의 공격력 (투사체 데미지)")] public float attackDamage = 10f;
    [Tooltip("공격 쿨타임 (초)")] public float attackCooldown = 1.5f;
    [Tooltip("투사체 발사 시 이동 속도")] public float projectileSpeed = 8f;
    [Tooltip("Projectile 투사체 프리팹. 생성 시 공통 패링/피해 판정을 자동 연결합니다.")]
    public GameObject projectilePrefab;
    [Tooltip("투사체가 발사되는 위치 (빈 오브젝트 할당, 없으면 적 중심에서 발사)")] 
    public Transform firePoint;

    public enum AttackPattern { SingleShot, SpreadShot }
    [Header("Attack Pattern Settings")]
    [Tooltip("공격 패턴 선택 (SingleShot: 단발, SpreadShot: 산탄)")]
    public AttackPattern attackPattern = AttackPattern.SingleShot;
    [Tooltip("SpreadShot일 경우 발사할 투사체 개수")] 
    public int spreadCount = 3;
    [Tooltip("SpreadShot일 경우 투사체 간의 각도 차이")] 
    public float spreadAngle = 15f;

    [Header("Range Settings (Box)")]
    [Tooltip("순찰 및 감지 사각형 범위 (가로, 세로)")] 
    public Vector2 patrolAreaSize = new Vector2(12f, 12f);
    [Tooltip("플레이어 공격 가능 사각형 범위 (가로, 세로) - 원거리이므로 순찰 범위보다 작거나 같게 설정")] 
    public Vector2 attackAreaSize = new Vector2(8f, 8f);

    [Header("References")]
    [Tooltip("씬의 플레이어입니다. 비우거나 프리팹 에셋이 연결되면 활성 PlayerMove를 자동 검색합니다.")]
    public Transform player;

    [Header("Animation Playback")]
    [Tooltip("프리팹 루트의 Animator입니다. 비우면 같은 오브젝트에서 찾기만 합니다. Controller/클립/전이는 코드로 생성하거나 변경하지 않습니다.")]
    public Animator animator;
    [Min(.05f), Tooltip("Death 클립이 아직 비어 있거나 Animator가 없을 때만 사용하는 사망 대기 시간입니다.")]
    public float fallbackDeathDuration = .75f;

    private float currentHealth;
    private float attackTimer;
    private Vector2 patrolCenter;
    private Vector2 targetPatrolPoint;
    private bool dead;
    private float nextPlayerSearch;
    private float deathElapsed;
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private bool HasAnimator => animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null;
    public float CurrentHealth => currentHealth;
    public bool IsDead => dead;
    
    private enum State { Patrol, Chase, Attack }
    private State currentState;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        currentHealth = Mathf.Max(1f, maxHealth);
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (gameObject.layer == 0 && enemyLayer >= 0) gameObject.layer = enemyLayer;
        // 예전 씬에도 피격 판정이 필요합니다. 이미 배치한 Collider는 유지합니다.
        if (GetComponentInChildren<Collider2D>(true) == null)
        {
            var shape = gameObject.AddComponent<BoxCollider2D>();
            shape.isTrigger = true;
            shape.size = Vector2.one;
            var visual = GetComponent<SpriteRenderer>();
            if (visual != null && visual.sprite != null)
            {
                shape.size = visual.sprite.bounds.size;
                shape.offset = visual.sprite.bounds.center;
            }
        }
    }

    void Start()
    {
        patrolCenter = transform.position; //시작 위치를 순찰 중심점으로 고정
        SetNewPatrolPoint();
        currentState = State.Patrol;

        ResolvePlayer();
    }

    private void ResolvePlayer()
    {
        if (player != null && player.gameObject.scene.IsValid() && player.gameObject.activeInHierarchy) return;
        player = null;
        if (Time.time < nextPlayerSearch) return;
        nextPlayerSearch = Time.time + 1f;
        var activePlayer = FindFirstObjectByType<PlayerMove>();
        if (activePlayer != null) player = activePlayer.transform;
    }

    void Update()
    {
        if (dead) { UpdateDeathAnimation(); return; }
        ResolvePlayer();
        if (player == null) { SetMoving(false); return; }

        Vector3 previousPosition = transform.position;

        //공격 쿨타임 감소
        if (attackTimer > 0) attackTimer -= Time.deltaTime;

        //상태 머신 실행
        switch (currentState)
        {
            case State.Patrol:
                PatrolLogic();
                break;
            case State.Chase:
                ChaseLogic();
                break;
            case State.Attack:
                AttackLogic();
                break;
        }
        SetMoving((transform.position - previousPosition).sqrMagnitude > .000001f);
    }

    //1. 순찰 로직
    void SetNewPatrolPoint()
    {
        float randomX = Random.Range(-patrolAreaSize.x / 2f, patrolAreaSize.x / 2f);
        float randomY = Random.Range(-patrolAreaSize.y / 2f, patrolAreaSize.y / 2f);
        targetPatrolPoint = patrolCenter + new Vector2(randomX, randomY);
    }

    void PatrolLogic()
    {
        //감지 범위 내에 플레이어가 들어오면 추적 시작
        if (IsInsideBox(player.position, patrolAreaSize))
        {
            currentState = State.Chase;
            return;
        }

        transform.position = Vector2.MoveTowards(transform.position, targetPatrolPoint, moveSpeed * Time.deltaTime);

        //목표 지점 도달 시 새 목표 설정
        if (Vector2.Distance(transform.position, targetPatrolPoint) < 0.1f)
        {
            SetNewPatrolPoint();
        }
    }

    //2. 추적 로직
    void ChaseLogic()
    {
        //감지 범위를 벗어나면 순찰로 복귀
        if (!IsInsideBox(player.position, patrolAreaSize))
        {
            currentState = State.Patrol;
            SetNewPatrolPoint();
            return;
        }

        //공격 범위 내에 들어오면 공격 상태로 전환
        if (IsInsideBox(player.position, attackAreaSize))
        {
            currentState = State.Attack;
            return;
        }

        //플레이어 방향으로 이동
        transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
    }

    //3. 공격 로직
    void AttackLogic()
    {
        //공격 범위를 벗어나면 다시 추적
        if (!IsInsideBox(player.position, attackAreaSize))
        {
            currentState = State.Chase;
            return;
        }

        //쿨타임 체크 후 투사체 발사
        if (attackTimer <= 0)
        {
            FireProjectile();
            attackTimer = attackCooldown;
        }
    }

    //투사체 발사 처리 (패턴 분기)
    void FireProjectile()
    {
        if (dead || projectilePrefab == null || player == null) return;
        if (HasAnimator) animator.SetTrigger(AttackHash);

        Vector2 spawnPos = firePoint != null ? firePoint.position : transform.position;
        Vector2 directionToPlayer = (player.position - (Vector3)spawnPos).normalized;

        switch (attackPattern)
        {
            case AttackPattern.SingleShot: //단발 사격
                SpawnProjectile(spawnPos, directionToPlayer);
                break;

            case AttackPattern.SpreadShot: //산탄 사격
                float startAngle = -spreadAngle * (spreadCount - 1) / 2f;
                for (int i = 0; i < spreadCount; i++)
                {
                    float currentAngle = startAngle + (spreadAngle * i);
                    //플레이어 방향을 기준으로 각도를 회전시켜 새로운 방향 벡터 계산
                    Vector2 spreadDir = Quaternion.Euler(0, 0, currentAngle) * directionToPlayer;
                    SpawnProjectile(spawnPos, spreadDir);
                }
                break;
        }
    }

    //투사체 생성 및 초기화
    void SpawnProjectile(Vector2 pos, Vector2 direction)
    {
        GameObject projObj = Instantiate(projectilePrefab, pos, Quaternion.identity);
        Projectile projScript = projObj.GetComponent<Projectile>();
        if (projScript == null) projScript = projObj.AddComponent<Projectile>();
        projScript.Initialize(gameObject, direction, projectileSpeed, attackDamage);
        projObj.SetActive(true);
    }

    //AABB 사각형 범위 체크 헬퍼
    bool IsInsideBox(Vector3 targetPos, Vector2 boxSize)
    {
        Vector3 pos = transform.position;
        return targetPos.x >= pos.x - boxSize.x / 2f && targetPos.x <= pos.x + boxSize.x / 2f &&
               targetPos.y >= pos.y - boxSize.y / 2f && targetPos.y <= pos.y + boxSize.y / 2f;
    }

    //피격 처리 (Player 등 외부 공격에 의해 호출됨)
    //플레이어의 무기나 스킬이 OnTriggerEnter2D 등을 통해 이 함수를 호출해야 함
    public void TakeDamage(float damage) => ReceiveDamage(damage, null, true);

    public bool ReceiveDamage(float damage, GameObject source, bool canReflect)
    {
        if (dead || !isActiveAndEnabled || damage <= 0f) return false;
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        DamageBlink.Play(gameObject);
        Debug.Log($"적 피격! 현재 체력: {currentHealth}");

        //데미지를 받으면 무조건 플레이어를 추적하도록 상태 변경 (선택 사항)
        if (currentState == State.Patrol) currentState = State.Chase;

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (HasAnimator)
        {
            animator.ResetTrigger(AttackHash);
            animator.SetTrigger(HitHash);
        }
        return true;
    }

    void Die()
    {
        if (dead) return;
        dead = true;
        foreach (var shape in GetComponentsInChildren<Collider2D>()) shape.enabled = false;
        Debug.Log("원거리 적 사망!");
        deathElapsed = 0f;
        if (HasAnimator)
        {
            animator.SetBool(IsMovingHash, false);
            animator.ResetTrigger(AttackHash);
            animator.ResetTrigger(HitHash);
            animator.SetBool(IsDeadHash, true);
        }
    }

    private void SetMoving(bool moving)
    {
        if (HasAnimator) animator.SetBool(IsMovingHash, moving);
    }

    private void UpdateDeathAnimation()
    {
        deathElapsed += Time.deltaTime;
        if (HasAnimator)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("Death") && state.length > .0001f)
            {
                if (state.normalizedTime >= 1f) Destroy(gameObject);
                return;
            }
        }
        if (deathElapsed >= Mathf.Max(.05f, fallbackDeathDuration)) Destroy(gameObject);
    }

    //기즈모 그리기 (인스펙터 선택 시에만 보이게 하려면 OnDrawGizmosSelected 사용 권장)
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(patrolAreaSize.x, patrolAreaSize.y, 0f));

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(attackAreaSize.x, attackAreaSize.y, 0f));
    }
}
