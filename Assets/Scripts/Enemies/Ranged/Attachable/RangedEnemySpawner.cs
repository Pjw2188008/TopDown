using UnityEngine;

/// <summary>감지 사각형 진입 시 RangedEnemy 또는 RangedEnemyController 프리팹 한 마리를 생성합니다.</summary>
[DisallowMultipleComponent]
public sealed class RangedEnemySpawner : MonoBehaviour
{
    // 기존 Component 참조를 보존해 GameObject 프리팹 항목으로 이전합니다.
    [SerializeField, HideInInspector] private RangedEnemy enemyPrefab;
    [SerializeField, InspectorName("Enemy Prefab"), Tooltip("Project 창의 원거리 몬스터 프리팹을 넣으세요. 루트에 활성 RangedEnemy 또는 RangedEnemyController 중 하나가 있어야 합니다. 기존 프리팹 연결은 자동 보존됩니다.")]
    private GameObject monsterPrefab;
    [Tooltip("감지할 플레이어입니다. 비어 있으면 PlayerMove를 자동 검색합니다.")]
    [SerializeField] private Transform player;
    [Tooltip("스포너를 중심으로 하는 감지 사각형의 전체 가로(X)·세로(Y) 크기입니다. 월드 단위이며 회전과 Scale의 영향을 받지 않습니다. 플레이어 중심이 테두리 안에 들어오면 생성합니다.")]
    [SerializeField] private Vector2 activationSize = new Vector2(14f, 14f);
    [Tooltip("실제 생성 위치입니다. 비어 있으면 스포너 위치에서 생성합니다.")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("기본값은 한 번만 생성입니다. 끄면 처치 후 플레이어가 감지 범위를 나갔다 다시 들어올 때 새로 생성합니다.")]
    [SerializeField] private bool spawnOnce = true;
    private GameObject spawnedEnemy;
    private bool hasSpawned, wasInside;
    private float nextPlayerSearch;
    public GameObject SpawnedEnemy => IsDefeated(spawnedEnemy) ? null : spawnedEnemy;
    public bool HasSpawned => hasSpawned;

    /// <summary>전투 구역 입장 시 감지 범위와 무관하게 한 번 생성합니다. 기존 적/Spawn Once 설정은 유지합니다.</summary>
    public GameObject SpawnForCombatArea(Transform target)
    {
        if (SpawnedEnemy != null) return SpawnedEnemy;
        if (spawnOnce && hasSpawned) return null;
        if (target == null || !IsSupportedPrefab(PrefabRoot)) return null;
        player = target;
        SpawnEnemy();
        return SpawnedEnemy;
    }

    public bool CanSpawnForCombatArea => IsSupportedPrefab(PrefabRoot) || SpawnedEnemy != null || (spawnOnce && hasSpawned);
    private GameObject PrefabRoot => monsterPrefab != null ? monsterPrefab : enemyPrefab != null ? enemyPrefab.gameObject : null;

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
        if (inside && !wasInside && SpawnedEnemy == null && (!spawnOnce || !hasSpawned))
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

    private void Awake() => MigrateLegacyPrefab();
    private void OnValidate() { activationSize = ActivationSize; MigrateLegacyPrefab(); }

    private void MigrateLegacyPrefab()
    {
        if (monsterPrefab == null && enemyPrefab != null) monsterPrefab = enemyPrefab.gameObject;
        if (monsterPrefab != null) enemyPrefab = null;
    }

    private void FindPlayer()
    {
        if (player != null || Time.time < nextPlayerSearch) return;

        nextPlayerSearch = Time.time + .5f;
        var found = FindFirstObjectByType<PlayerMove>();
        if (found != null) player = found.transform;
    }

    private void SpawnEnemy()
    {
        if (!IsSupportedPrefab(PrefabRoot))
        {
            Debug.LogWarning("Enemy Prefab 루트에 활성 RangedEnemy 또는 RangedEnemyController 중 하나가 필요합니다. Project 창의 원거리 프리팹을 넣으세요.", this);
            return;
        }

        Transform point = spawnPoint != null ? spawnPoint : transform;
        spawnedEnemy = Instantiate(PrefabRoot, point.position, Quaternion.identity);
        spawnedEnemy.SetActive(true);
        if (spawnedEnemy.TryGetComponent<RangedEnemy>(out var original)) original.SetTarget(player);
        if (spawnedEnemy.TryGetComponent<RangedEnemyController>(out var other)) other.player = player;
        hasSpawned = true;
    }

    private static bool IsSupportedPrefab(GameObject prefab)
    {
        if (prefab == null) return false;
        var original = prefab.GetComponent<RangedEnemy>();
        var other = prefab.GetComponent<RangedEnemyController>();
        return original != null && original.enabled && other == null
            || other != null && other.enabled && original == null;
    }

    /// <summary>사망 애니메이션 중에도 처치로 인정합니다. 살아 있는 적의 비활성화는 처치가 아닙니다.</summary>
    public static bool IsDefeated(GameObject enemy)
    {
        if (enemy == null) return true;
        if (enemy.TryGetComponent<RangedEnemy>(out var original)) return original.IsDead;
        return enemy.TryGetComponent<RangedEnemyController>(out var other) && other.IsDead;
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
