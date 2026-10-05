using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>전투 구역 입장 후 벽을 닫고 등록된 적이 모두 제거되면 엽니다. 구역 루트에 부착합니다.</summary>
[DisallowMultipleComponent]
public sealed class CombatAreaLock : MonoBehaviour
{
    [Header("입장 범위")]
    [Tooltip("입장을 감지할 BoxCollider2D입니다. Is Trigger를 켜고 직접 범위를 조절하세요. 벽 Collider와는 별개입니다.")]
    [SerializeField] private BoxCollider2D entryArea;
    [Tooltip("비우면 활성 PlayerMove를 검색합니다. 플레이어 몸 전체가 범위 안에 들어오면 벽을 닫아 입구 끼임을 줄입니다.")]
    [SerializeField] private PlayerMove player;
    [Header("막을 벽")]
    [Tooltip("잠글 때 활성화할 벽 오브젝트입니다. 일반 Collider2D(Is Trigger OFF)와 원하는 SpriteRenderer를 넣으세요. 시작/클리어 시 비활성화됩니다. 플레이어/구역 루트를 넣지 마세요.")]
    [SerializeField] private GameObject[] barriers = new GameObject[0];
    [Header("전투 대상")]
    [Tooltip("켜면 입장 시 범위 안의 근접/원거리 적과 활성 근접/원거리 스포너를 한 번 수집합니다. 이후 범위 밖으로 나간 적도 처치 대상으로 유지합니다.")]
    [SerializeField] private bool collectEnemiesInArea = true;
    [Tooltip("추가로 추적할 적의 루트입니다. 현재 적들은 사망하면 루트를 Destroy하므로 제거를 전멸 기준으로 사용합니다. 단순 비활성화는 처치가 아닙니다.")]
    [SerializeField] private GameObject[] enemies = new GameObject[0];
    [Tooltip("입장 시 즉시 생성시킬 원거리 스포너입니다. 전투 중 자동 재생성은 잠시 중지하고 종료 시 원래 enabled 상태로 복원합니다. 여러 전투 구역이 같은 스포너를 공유하지 마세요.")]
    [SerializeField] private RangedEnemySpawner[] spawners = new RangedEnemySpawner[0];
    [Tooltip("입장 시 생성하고 전멸 대상으로 추적할 근접 스포너입니다. 자동 수집을 켜면 구역 안의 활성 스포너는 직접 등록하지 않아도 됩니다. 여러 전투 구역과 공유하지 마세요.")]
    [SerializeField] private MeleeEnemySpawner[] meleeSpawners = new MeleeEnemySpawner[0];
    [Header("선택 이벤트")]
    [SerializeField] private UnityEvent onLocked = new UnityEvent();
    [SerializeField] private UnityEvent onCleared = new UnityEvent();

    private readonly HashSet<GameObject> remaining = new HashSet<GameObject>();
    private readonly Dictionary<Behaviour, bool> controlledSpawners = new Dictionary<Behaviour, bool>();
    private readonly List<Collider2D> playerColliders = new List<Collider2D>();
    private bool wasInside, warned;
    private float nextPlayerSearch;
    public bool IsLocked { get; private set; }
    public bool IsCleared { get; private set; }
    public int RemainingEnemyCount => remaining.Count;

    private void Awake() => SetBarriers(false);

    private void LateUpdate()
    {
        if (Time.timeScale <= 0f || IsCleared) return;
        if (player == null && Time.unscaledTime >= nextPlayerSearch)
        {
            player = FindFirstObjectByType<PlayerMove>();
            nextPlayerSearch = Time.unscaledTime + .5f;
        }
        if (entryArea == null || !entryArea.enabled || !entryArea.gameObject.activeInHierarchy)
        {
            if (IsLocked) CancelEncounter();
            return;
        }
        Physics2D.SyncTransforms();
        bool inside = player != null && player.gameObject.activeInHierarchy && entryArea.OverlapPoint(player.transform.position);
        if (IsLocked)
        {
            remaining.RemoveWhere(enemy => enemy == null);
            if (remaining.Count == 0) { CompleteEncounter(); return; }
            // 즉사 후 시작점 복귀/외부 텔레포트로 전투 지역에 갇히지 않게 문을 열고 재입장을 허용합니다.
            if (!inside) CancelEncounter();
            return;
        }
        bool fullyInside = inside && IsPlayerFullyInside();
        if (fullyInside && !wasInside) BeginEncounter();
        wasInside = fullyInside;
    }

    private bool IsPlayerFullyInside()
    {
        player.GetComponentsInChildren(false, playerColliders);
        foreach (var shape in playerColliders)
        {
            if (!shape.enabled || shape.isTrigger) continue;
            Bounds b = shape.bounds;
            if (!entryArea.OverlapPoint(new Vector2(b.min.x, b.min.y))
                || !entryArea.OverlapPoint(new Vector2(b.min.x, b.max.y))
                || !entryArea.OverlapPoint(new Vector2(b.max.x, b.min.y))
                || !entryArea.OverlapPoint(new Vector2(b.max.x, b.max.y))) return false;
        }
        return true;
    }

    private void BeginEncounter()
    {
        if (!HasSafeBarriers())
        {
            Warn("CombatAreaLock: 벽 오브젝트와 일반 Collider2D를 지정하세요. 잘못된 구성에서는 잠그지 않습니다.");
            return;
        }
        remaining.Clear();
        var sources = new HashSet<RangedEnemySpawner>();
        var meleeSources = new HashSet<MeleeEnemySpawner>();
        foreach (var enemy in enemies) if (enemy != null) remaining.Add(enemy);
        foreach (var source in spawners) if (source != null) sources.Add(source);
        foreach (var source in meleeSpawners) if (source != null) meleeSources.Add(source);
        if (collectEnemiesInArea)
        {
            Collect<EnemyController>(); Collect<MeleeEnemy>(); Collect<RangedEnemy>();
            foreach (var source in FindObjectsByType<RangedEnemySpawner>(FindObjectsSortMode.None))
                if (source.enabled && entryArea.OverlapPoint(source.transform.position)) sources.Add(source);
            foreach (var source in FindObjectsByType<MeleeEnemySpawner>(FindObjectsSortMode.None))
                if (source.enabled && entryArea.OverlapPoint(source.transform.position)) meleeSources.Add(source);
        }
        foreach (var source in sources)
        {
            if (!source.gameObject.activeInHierarchy || !source.CanSpawnForCombatArea)
            {
                Warn("CombatAreaLock: 연결된 스포너가 비활성 상태이거나 몬스터 프리팹이 없습니다. 벽을 닫지 않습니다.");
                remaining.Clear(); return;
            }
        }
        // 모든 종류의 스포너를 검증한 다음에만 생성/중지합니다. 일부만 생성되는 실패를 방지합니다.
        foreach (var source in meleeSources)
        {
            if (!source.gameObject.activeInHierarchy || !source.CanSpawnForCombatArea)
            {
                Warn("CombatAreaLock: 연결된 근접 스포너가 비활성 상태이거나 몬스터 프리팹이 없습니다. 벽을 닫지 않습니다.");
                remaining.Clear(); return;
            }
        }
        foreach (var source in sources)
        {
            controlledSpawners.Add(source, source.enabled);
            source.enabled = false;
            var enemy = source.SpawnForCombatArea(player.transform);
            if (enemy != null) remaining.Add(enemy.gameObject);
        }
        foreach (var source in meleeSources)
        {
            controlledSpawners.Add(source, source.enabled);
            source.enabled = false;
            var enemy = source.SpawnForCombatArea(player.transform);
            if (enemy != null) remaining.Add(enemy.gameObject);
        }
        if (remaining.Count == 0) { CompleteEncounter(); return; }
        IsLocked = true;
        SetBarriers(true);
        onLocked.Invoke();
    }

    private void Collect<T>() where T : Component
    {
        foreach (var enemy in FindObjectsByType<T>(FindObjectsSortMode.None))
            if (entryArea.OverlapPoint(enemy.transform.position)) remaining.Add(enemy.gameObject);
    }

    private bool HasSafeBarriers()
    {
        if (barriers == null || barriers.Length == 0) return false;
        foreach (var wall in barriers)
        {
            if (!IsSafeBarrier(wall)) return false;
            bool solid = false;
            foreach (var shape in wall.GetComponentsInChildren<Collider2D>(true))
                if (shape.enabled && !shape.isTrigger) { solid = true; break; }
            if (!solid) return false;
        }
        return true;
    }

    private bool IsSafeBarrier(GameObject wall) => wall != null && !transform.IsChildOf(wall.transform)
        && (player == null || !player.transform.IsChildOf(wall.transform))
        && wall.GetComponentInChildren<PlayerMove>(true) == null;

    private void SetBarriers(bool active)
    {
        if (barriers != null)
            foreach (var wall in barriers) if (IsSafeBarrier(wall)) wall.SetActive(active);
        Physics2D.SyncTransforms();
    }

    private void RestoreSpawners()
    {
        foreach (var pair in controlledSpawners) if (pair.Key != null) pair.Key.enabled = pair.Value;
        controlledSpawners.Clear();
    }

    private void CompleteEncounter()
    {
        IsLocked = false; IsCleared = true;
        remaining.Clear(); SetBarriers(false); RestoreSpawners();
        onCleared.Invoke();
    }

    private void CancelEncounter()
    {
        IsLocked = false; wasInside = false;
        remaining.Clear(); SetBarriers(false); RestoreSpawners();
    }

    private void OnDisable() => CancelEncounter();
    private void Warn(string message) { if (!warned) { warned = true; Debug.LogWarning(message, this); } }

    private void OnDrawGizmosSelected()
    {
        if (entryArea == null) return;
        var matrix = Gizmos.matrix; var color = Gizmos.color;
        Gizmos.matrix = entryArea.transform.localToWorldMatrix;
        Gizmos.color = IsLocked ? Color.red : Color.cyan;
        Gizmos.DrawWireCube(entryArea.offset, entryArea.size);
        Gizmos.matrix = matrix; Gizmos.color = color;
    }
}
