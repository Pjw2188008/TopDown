using UnityEngine;

/// <summary>플레이어가 감지 원에 들어오면 원거리 적 한 마리를 생성합니다. RangedEnemySpawner 프리팹에 부착합니다.</summary>
[DisallowMultipleComponent]
public sealed class RangedEnemySpawner : MonoBehaviour
{
    [Tooltip("생성할 원거리 몬스터 프리팹입니다. 씬의 몬스터가 아닌 Project 창 프리팹을 넣으세요.")]
    [SerializeField] private RangedEnemy enemyPrefab;
    [Tooltip("감지할 플레이어입니다. 비어 있으면 PlayerMove를 자동 검색합니다.")]
    [SerializeField] private Transform player;
    [Tooltip("스포너 중심에서 이 거리 안으로 플레이어가 들어오면 생성합니다.")]
    [SerializeField, Min(.1f)] private float activationRadius = 7f;
    [Tooltip("실제 생성 위치입니다. 비어 있으면 스포너 위치에서 생성합니다.")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("기본값은 한 번만 생성입니다. 끄면 처치 후 플레이어가 감지 범위를 나갔다 다시 들어올 때 새로 생성합니다.")]
    [SerializeField] private bool spawnOnce = true;
    private RangedEnemy spawnedEnemy;
    private bool hasSpawned, wasInside;
    private float nextPlayerSearch;
    public RangedEnemy SpawnedEnemy => spawnedEnemy;
    public bool HasSpawned => hasSpawned;

    private void Update()
    {
        if (Time.timeScale <= 0f) return;

        FindPlayer();
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            wasInside = false;
            return;
        }

        bool inside = ((Vector2)(player.position - transform.position)).sqrMagnitude <= activationRadius * activationRadius;
        if (inside && !wasInside && spawnedEnemy == null && (!spawnOnce || !hasSpawned))
        {
            SpawnEnemy();
        }
        wasInside = inside;
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
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(.1f, activationRadius));
        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(position, Vector3.one * .7f);
        if (spawnPoint != null) Gizmos.DrawLine(transform.position, position);
    }
}
