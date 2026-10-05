using UnityEngine;

/// <summary>사각 감지 범위 진입 시 EnemyController 근접 몬스터 한 마리를 생성합니다. 빈 스포너 오브젝트에 부착하세요.</summary>
[DisallowMultipleComponent]
public sealed class MeleeEnemySpawner : MonoBehaviour
{
    [Tooltip("생성할 근접 몬스터 프리팹입니다. Project의 MeleeMonster 프리팹을 넣으세요.")]
    [SerializeField] private EnemyController enemyPrefab;
    [Tooltip("비우면 활성 PlayerMove를 자동 검색합니다.")]
    [SerializeField] private Transform player;
    [Tooltip("감지 사각형의 전체 가로(X)/세로(Y), 월드 단위입니다. 회전/Scale은 반영하지 않으며 선택 시 청록색 기즈모로 표시됩니다.")]
    [SerializeField] private Vector2 activationSize = new Vector2(14f, 14f);
    [Tooltip("실제 생성 위치입니다. 자식 SpawnPoint를 이동하세요. 비우면 스포너 중심에 생성합니다.")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("켜면 한 번만 생성합니다. 끄면 처치 후 범위를 나갔다 다시 들어올 때 재생성합니다. 살아 있는 동안은 중복 생성하지 않습니다.")]
    [SerializeField] private bool spawnOnce = true;

    private EnemyController spawnedEnemy;
    private bool hasSpawned, wasInside;
    private float nextPlayerSearch;
    public EnemyController SpawnedEnemy => spawnedEnemy;
    public bool HasSpawned => hasSpawned;
    public bool CanSpawnForCombatArea => enemyPrefab != null || spawnedEnemy != null || (spawnOnce && hasSpawned);
    private Vector2 ActivationSize => new Vector2(Mathf.Max(.1f, activationSize.x), Mathf.Max(.1f, activationSize.y));

    /// <summary>전투 구역 입장 시 생성합니다. 이미 생성한 적과 1회 생성 제한은 유지합니다.</summary>
    public EnemyController SpawnForCombatArea(Transform target)
    {
        if (spawnedEnemy != null) return spawnedEnemy;
        if (spawnOnce && hasSpawned) return null;
        if (target == null || enemyPrefab == null) return null;
        player = target;
        SpawnEnemy();
        return spawnedEnemy;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;
        if (player == null && Time.time >= nextPlayerSearch)
        {
            nextPlayerSearch = Time.time + .5f;
            var found = FindFirstObjectByType<PlayerMove>();
            if (found != null) player = found.transform;
        }
        if (player == null || !player.gameObject.activeInHierarchy) { wasInside = false; return; }
        bool inside = IsInsideActivationArea(player.position);
        if (inside && !wasInside && spawnedEnemy == null && (!spawnOnce || !hasSpawned)) SpawnEnemy();
        wasInside = inside;
    }

    private bool IsInsideActivationArea(Vector3 position)
    {
        Vector2 offset = position - transform.position;
        Vector2 halfSize = ActivationSize * .5f;
        return Mathf.Abs(offset.x) <= halfSize.x && Mathf.Abs(offset.y) <= halfSize.y;
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null) { Debug.LogWarning("근접 몬스터 프리팹을 지정하세요.", this); return; }
        Transform point = spawnPoint != null ? spawnPoint : transform;
        spawnedEnemy = Instantiate(enemyPrefab, point.position, Quaternion.identity);
        spawnedEnemy.player = player;
        hasSpawned = true;
    }

    private void OnValidate() => activationSize = ActivationSize;

    private void OnDrawGizmosSelected()
    {
        var matrix = Gizmos.matrix; var color = Gizmos.color;
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.cyan;
        Vector2 size = ActivationSize;
        Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0f));
        Vector3 point = spawnPoint != null ? spawnPoint.position : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(point, Vector3.one * .7f);
        if (spawnPoint != null) Gizmos.DrawLine(transform.position, point);
        Gizmos.matrix = matrix; Gizmos.color = color;
    }
}
