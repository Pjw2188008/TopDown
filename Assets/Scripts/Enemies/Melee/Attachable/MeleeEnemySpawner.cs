using UnityEngine;
using CheolO.Monsters;

/// <summary>사각 범위 진입 시 EnemyController 또는 TriangleMeleeMonster 프리팹 하나를 생성합니다. 빈 스포너에 부착하세요.</summary>
[DisallowMultipleComponent]
public sealed class MeleeEnemySpawner : MonoBehaviour
{
    // 기존 씬/프리팹의 Component 참조를 유지한 채 새 GameObject 항목으로 이전합니다.
    [SerializeField, HideInInspector] private EnemyController enemyPrefab;
    [SerializeField, InspectorName("Enemy Prefab"), Tooltip("Project의 MeleeMonster 또는 mon_1 프리팹을 넣으세요. 루트에 EnemyController / TriangleMeleeMonster 중 하나만 있어야 합니다. 기존 연결은 자동 보존됩니다.")]
    private GameObject monsterPrefab;
    [Tooltip("비우면 활성 PlayerMove를 자동 검색합니다.")]
    [SerializeField] private Transform player;
    [Tooltip("감지 사각형의 전체 가로(X)/세로(Y), 월드 단위입니다. 회전/Scale은 반영하지 않으며 선택 시 청록색 기즈모로 표시됩니다.")]
    [SerializeField] private Vector2 activationSize = new Vector2(14f, 14f);
    [Tooltip("실제 생성 위치입니다. 자식 SpawnPoint를 이동하세요. 비우면 스포너 중심에 생성합니다.")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("켜면 한 번만 생성합니다. 끄면 처치 후 범위를 나갔다 다시 들어올 때 재생성합니다. 살아 있는 동안은 중복 생성하지 않습니다.")]
    [SerializeField] private bool spawnOnce = true;

    private GameObject spawnedEnemy;
    private bool hasSpawned, wasInside;
    private float nextPlayerSearch;
    private bool combatAreaCleared;
    public bool CanSpawnWave => IsSupportedPrefab(PrefabRoot);

    /// <summary>구역이 소유하는 웨이브 생성입니다. Spawn Once와 기존 생존 개체를 무시하고 한 마리를 추가합니다.</summary>
    public GameObject SpawnForCombatWave(Transform target, Vector3? position = null)
    {
        if (target == null || !CanSpawnWave) return null;
        player = target;
        var previous = spawnedEnemy;
        SpawnEnemy();
        if (spawnedEnemy == previous || spawnedEnemy == null) return null;
        if (position.HasValue) spawnedEnemy.transform.position = position.Value;
        return spawnedEnemy;
    }

    /// <summary>웨이브 구역 클리어 후 재입장해도 자동 생성하지 않습니다. 씬 재시작 시 초기화됩니다.</summary>
    public void MarkCombatAreaCleared() => combatAreaCleared = true;
    public GameObject SpawnedEnemy => IsDefeated(spawnedEnemy) ? null : spawnedEnemy;
    public bool HasSpawned => hasSpawned;
    public bool CanSpawnForCombatArea => SpawnedEnemy != null || (spawnOnce && hasSpawned) || IsSupportedPrefab(PrefabRoot);
    private GameObject PrefabRoot => monsterPrefab != null ? monsterPrefab : enemyPrefab != null ? enemyPrefab.gameObject : null;
    private Vector2 ActivationSize => new Vector2(Mathf.Max(.1f, activationSize.x), Mathf.Max(.1f, activationSize.y));

    /// <summary>전투 구역 입장 시 생성합니다. 이미 생성한 적과 1회 생성 제한은 유지합니다.</summary>
    public GameObject SpawnForCombatArea(Transform target)
    {
        if (SpawnedEnemy != null) return SpawnedEnemy;
        if (spawnOnce && hasSpawned) return null;
        if (target == null || !IsSupportedPrefab(PrefabRoot)) return null;
        player = target;
        SpawnEnemy();
        return SpawnedEnemy;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || combatAreaCleared || CombatAreaLock.IsWaveManaged(this)) return;
        if (player == null && Time.time >= nextPlayerSearch)
        {
            nextPlayerSearch = Time.time + .5f;
            var found = FindFirstObjectByType<PlayerMove>();
            if (found != null) player = found.transform;
        }
        if (player == null || !player.gameObject.activeInHierarchy) { wasInside = false; return; }
        bool inside = IsInsideActivationArea(player.position);
        if (inside && !wasInside && SpawnedEnemy == null && (!spawnOnce || !hasSpawned)) SpawnEnemy();
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
        var prefab = PrefabRoot;
        if (!IsSupportedPrefab(prefab))
        { Debug.LogWarning("Enemy Prefab 루트에 활성 EnemyController 또는 TriangleMeleeMonster가 하나만 필요합니다. MeleeMonster / mon_1 프리팹을 지정하세요.", this); return; }
        var targetPlayer = player != null ? player.GetComponentInParent<PlayerMove>() : null;
        if (prefab.GetComponent<TriangleMeleeMonster>() != null && targetPlayer == null)
        { Debug.LogWarning("mon_1의 대상으로 PlayerMove가 있는 플레이어를 연결하세요.", this); return; }
        Transform point = spawnPoint != null ? spawnPoint : transform;
        spawnedEnemy = Instantiate(prefab, point.position, Quaternion.identity);
        var original = spawnedEnemy.GetComponent<EnemyController>();
        if (original != null) original.player = player;
        var triangle = spawnedEnemy.GetComponent<TriangleMeleeMonster>();
        if (triangle != null) triangle.player = targetPlayer;
        spawnedEnemy.SetActive(true);
        hasSpawned = true;
    }

    private static bool IsSupportedPrefab(GameObject prefab)
    {
        if (prefab == null) return false;
        var original = prefab.GetComponent<EnemyController>();
        var triangle = prefab.GetComponent<TriangleMeleeMonster>();
        return original != null && original.enabled && triangle == null
            || triangle != null && triangle.enabled && original == null;
    }

    /// <summary>Triangle의 사망 애니메이션/시체가 남아 있어도 전멸과 재생성 조건에서 처치로 인식합니다. 단순 비활성화는 처치가 아닙니다.</summary>
    public static bool IsDefeated(GameObject enemy)
    {
        if (enemy == null) return true;
        var triangle = enemy.GetComponent<TriangleMeleeMonster>();
        return triangle != null && triangle.IsDead;
    }

    private void MigrateLegacyPrefab()
    {
        if (monsterPrefab == null && enemyPrefab != null) monsterPrefab = enemyPrefab.gameObject;
        if (monsterPrefab != null) enemyPrefab = null;
    }
    private void Awake() => MigrateLegacyPrefab();
    private void OnValidate() { activationSize = ActivationSize;MigrateLegacyPrefab(); }

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
