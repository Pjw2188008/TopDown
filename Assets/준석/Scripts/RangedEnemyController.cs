using UnityEngine;

public class RangedEnemyController : MonoBehaviour
{
    [Header("Stats Settings")]
    [Tooltip("적의 최대 체력")] public float maxHealth = 100f;
    [Tooltip("적의 이동 속도")] public float moveSpeed = 3f;

    [Header("Attack Settings")]
    [Tooltip("적의 공격력 (투사체 데미지)")] public float attackDamage = 10f;
    [Tooltip("공격 쿨타임 (초)")] public float attackCooldown = 1.5f;
    [Tooltip("투사체 발사 시 이동 속도")] public float projectileSpeed = 8f;
    [Tooltip("투사체 프리팹 (반드시 Projectile 스크립트가 붙어있어야 함)")] 
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
    [Tooltip("플레이어 오브젝트 (Player 태그가 있어야 함)")] 
    public Transform player;

    private float currentHealth;
    private float attackTimer;
    private Vector2 patrolCenter;
    private Vector2 targetPatrolPoint;
    
    private enum State { Patrol, Chase, Attack }
    private State currentState;

    void Start()
    {
        currentHealth = maxHealth;
        patrolCenter = transform.position; //시작 위치를 순찰 중심점으로 고정
        SetNewPatrolPoint();
        currentState = State.Patrol;

        //플레이어 자동 찾기 (인스펙터 할당을 깜빡했을 경우 대비)
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (currentHealth <= 0 || player == null) return;

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
        if (projectilePrefab == null) return;

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
        if (projScript != null)
        {
            //투사체가 날아갈 방향, 속도, 데미지 전달
            projScript.Initialize(direction, projectileSpeed, attackDamage);
        }
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
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"적 피격! 현재 체력: {currentHealth}");

        //데미지를 받으면 무조건 플레이어를 추적하도록 상태 변경 (선택 사항)
        if (currentState == State.Patrol) currentState = State.Chase;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("원거리 적 사망!");
        Destroy(gameObject);
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