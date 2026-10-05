using UnityEngine;

/// <summary>플레이어가 감지 사각형에 들어오면 원거리 적 한 마리를 생성합니다. RangedEnemySpawner 프리팹에 부착합니다.</summary>
[DisallowMultipleComponent]
public sealed class RangedEnemySpawner : MonoBehaviour
{
    [Tooltip("생성할 원거리 몬스터 프리팹입니다. 씬의 몬스터가 아닌 Project 창 프리팹을 넣으세요.")]
    [SerializeField] private RangedEnemy enemyPrefab;
    [Tooltip("감지할 플레이어입니다. 비어 있으면 PlayerMove를 자동 검색합니다.")]
    [SerializeField] private Transform player;
    [Tooltip("스포너를 중심으로 하는 감지 사각형의 전체 가로(X)·세로(Y) 크기입니다. 월드 단위이며 회전과 Scale의 영향을 받지 않습니다. 플레이어 중심이 테두리 안에 들어오면 생성합니다.")]
    [SerializeField] private Vector2 activationSize = new Vector2(14f, 14f);
    [Tooltip("실제 생성 위치입니다. 비어 있으면 스포너 위치에서 생성합니다.")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("기본값은 한 번만 생성입니다. 끄면 처치 후 플레이어가 감지 범위를 나갔다 다시 들어올 때 새로 생성합니다.")]
    [SerializeField] private bool spawnOnce = true;
    private RangedEnemy spawnedEnemy;
    private bool hasSpawned, wasInside;
    private float nextPlayerSearch;
    public RangedEnemy SpawnedEnemy => spawnedEnemy;
    public bool HasSpawned => hasSpawned;

    /// <summary>전투 구역 입장 시 감지 범위와 무관하게 한 번 생성합니다. 기존 적/Spawn Once 설정은 유지합니다.</summary>
    public RangedEnemy SpawnForCombatArea(Transform target)
    {
        if (spawnedEnemy != null) return spawnedEnemy;
        if (spawnOnce && hasSpawned) return null;
        if (target == null || enemyPrefab == null) return null;
        player = target;
        SpawnEnemy();
        return spawnedEnemy;
    }

    public bool CanSpawnForCombatArea => enemyPrefab != null || spawnedEnemy != null || (spawnOnce && hasSpawned);

    private void Update()
    {
        if (Time.timeScale <= 0f) return;

        FindPlayer();
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            wasInside = false;
            return;
        }

        bool inside = IsInsideActivationArea(player.position);
        if (inside && !wasInside && spawnedEnemy == null && (!spawnOnce || !hasSpawned))
        {
            SpawnEnemy();
        }
        wasInside = inside;
    }

    private Vector2 ActivationSize => new Vector2(Mathf.Max(.1f, activationSize.x), Mathf.Max(.1f, activationSize.y));

    private bool IsInsideActivationArea(Vector3 worldPosition)
    {
        Vector2 halfSize = ActivationSize * .5f;
        Vector2 offset = worldPosition - transform.position;
        return Mathf.Abs(offset.x) <= halfSize.x && Mathf.Abs(offset.y) <= halfSize.y;
    }

    private void OnValidate() => activationSize = ActivationSize;

    private void FindPlayer()
    {
        if (player != null || Time.time < nextPlayerSearch) return;

        nextPlayerSearch = Time.time + .5f;
        var found = FindFirstObjectByType<PlayerMove>();
        if (found != null) player = found.transform;
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("원거리 몬스터 프리팹을 지정하세요.", this);
            return;
        }

        Transform point = spawnPoint != null ? spawnPoint : transform;
        spawnedEnemy = Instantiate(enemyPrefab, point.position, Quaternion.identity);
        spawnedEnemy.SetTarget(player);
        hasSpawned = true;
    }

    private void OnDrawGizmosSelected()
    {
        Color previousColor = Gizmos.color;
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.cyan;
        Vector2 size = ActivationSize;
        Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0f));
        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(position, Vector3.one * .7f);
        if (spawnPoint != null) Gizmos.DrawLine(transform.position, position);
        Gizmos.color = previousColor;
        Gizmos.matrix = previousMatrix;
    }
}
