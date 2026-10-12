using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using CheolO.Monsters;

/// <summary>전투 구역 입장 후 벽을 닫고 등록된 적이 모두 제거되면 엽니다. 구역 루트에 부착합니다.</summary>
[DisallowMultipleComponent]
public sealed class CombatAreaLock : MonoBehaviour
{
    [System.Serializable]
    public sealed class WaveGroup
    {
        [Tooltip("이 종류를 생성할 씬의 원거리 스포너. 아래 Melee Spawner와 둘 중 하나만 지정하세요.")]
        public RangedEnemySpawner rangedSpawner;
        [Tooltip("이 종류를 생성할 씬의 근접 스포너. 위 Ranged Spawner와 둘 중 하나만 지정하세요.")]
        public MeleeEnemySpawner meleeSpawner;
        [Min(1), Tooltip("이번 웨이브에서 이 종류를 생성할 마릿수입니다.")]
        public int count = 1;
        public Behaviour Source => rangedSpawner != null ? (Behaviour)rangedSpawner : meleeSpawner;
    }

    [System.Serializable]
    public sealed class WaveConfiguration
    {
        [Tooltip("Inspector에서 구분하기 위한 이름입니다.")]
        public string waveName;
        [Tooltip("몬스터 종류별 스포너와 마릿수. 원거리/근접을 섞을 수 있습니다.")]
        public WaveGroup[] groups = new WaveGroup[0];
        [Tooltip("이 웨이브만의 생성 위치. 비우면 공통 Wave Spawn Points, 그것도 비우면 스포너 Spawn Point를 사용합니다. 그룹 순서대로 사용하며 위치 수가 부족하면 순환합니다.")]
        public Transform[] spawnPoints = new Transform[0];
    }

    private static readonly List<CombatAreaLock> activeAreas = new List<CombatAreaLock>();

    // 스포너 Update가 입장 처리보다 먼저 실행되어 목표 수보다 많이 생성되는 것을 방지합니다.
    public static bool IsWaveManaged(Behaviour source)
    {
        foreach (var area in activeAreas)
        {
            if (area == null || !area.isActiveAndEnabled || !area.useWaves) continue;
            if (area.HasCustomWaves)
                foreach (var wave in area.waveConfigurations)
                    if (wave != null && wave.groups != null)
                        foreach (var group in wave.groups)
                            if (group != null && (group.rangedSpawner == source || group.meleeSpawner == source)) return true;
            if (source is RangedEnemySpawner ranged && System.Array.IndexOf(area.spawners, ranged) >= 0) return true;
            if (source is MeleeEnemySpawner melee && System.Array.IndexOf(area.meleeSpawners, melee) >= 0) return true;
            if (area.collectEnemiesInArea && area.entryArea != null && area.entryArea.OverlapPoint(source.transform.position)) return true;
        }
        return false;
    }

    private void OnEnable() { if (!activeAreas.Contains(this)) activeAreas.Add(this); }
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
    [Tooltip("추가로 추적할 적의 루트입니다. 제거된 적 또는 IsDead가 된 Triangle/원거리 적을 처치로 인식합니다. 단순 비활성화는 처치가 아닙니다.")]
    [SerializeField] private GameObject[] enemies = new GameObject[0];
    [Tooltip("입장 시 즉시 생성시킬 원거리 스포너입니다. 전투 중 자동 재생성은 잠시 중지하고 종료 시 원래 enabled 상태로 복원합니다. 여러 전투 구역이 같은 스포너를 공유하지 마세요.")]
    [SerializeField] private RangedEnemySpawner[] spawners = new RangedEnemySpawner[0];
    [Tooltip("입장 시 생성하고 전멸 대상으로 추적할 근접 스포너입니다. 자동 수집을 켜면 구역 안의 활성 스포너는 직접 등록하지 않아도 됩니다. 여러 전투 구역과 공유하지 마세요.")]
    [SerializeField] private MeleeEnemySpawner[] meleeSpawners = new MeleeEnemySpawner[0];
    [Header("웨이브 전투 (선택)")]
    [Tooltip("켜면 웨이브 전멸 후 다음 몬스터를 생성하고 마지막 웨이브까지 완료해야 문이 열립니다. 끄면 기존 단일 전투 방식입니다.")]
    [SerializeField] private bool useWaves;
    [Min(1), Tooltip("최초 웨이브를 포함한 총 웨이브 수. 3이면 총 3번의 전멸이 필요합니다.")]
    [SerializeField] private int totalWaves = 3;
    [Min(1), Tooltip("구역 전체의 웨이브당 생성 목표 수. 등록한 근접/원거리 스포너를 번갈아 사용합니다. 이미 생성된 스포너 몬스터는 첫 웨이브 인원에 포함합니다. 직접 배치한 적은 별도 추가 대상입니다.")]
    [SerializeField] private int enemiesPerWave = 3;
    [Min(0), Tooltip("전멸 후 다음 웨이브까지 기다리는 게임 시간(초). 대기 중에도 문은 닫혀 있습니다.")]
    [SerializeField] private float waveDelay = 1f;
    [Tooltip("선택 사항: 웨이브 슬롯별 생성 위치입니다. 비우면 각 스포너의 Spawn Point를 사용합니다. 몬스터가 겹치지 않도록 인원 수만큼 안전한 위치를 넣으세요.")]
    [SerializeField] private Transform[] waveSpawnPoints = new Transform[0];
    [Tooltip("비우면 기존 Total Waves / Enemies Per Wave 방식입니다. 한 개 이상 넣으면 목록 순서가 웨이브 순서이고 목록 개수가 총 웨이브 수입니다. 각 Groups에 종류별 스포너/마릿수를 지정하세요. 이때 기존 공통 Spawners 목록 대신 이 목록의 스포너를 사용합니다.")]
    [SerializeField] private WaveConfiguration[] waveConfigurations = new WaveConfiguration[0];
    [Header("선택 이벤트")]
    [SerializeField] private UnityEvent onLocked = new UnityEvent();
    [SerializeField] private UnityEvent onCleared = new UnityEvent();

    private readonly HashSet<GameObject> remaining = new HashSet<GameObject>();
    private readonly Dictionary<Behaviour, bool> controlledSpawners = new Dictionary<Behaviour, bool>();
    private readonly List<Collider2D> playerColliders = new List<Collider2D>();
    private bool wasInside, warned;
    private float nextPlayerSearch;
    private readonly List<Behaviour> waveSources = new List<Behaviour>();
    private readonly HashSet<GameObject> waveOwnedEnemies = new HashSet<GameObject>();
    private bool waitingForWave;
    private float nextWaveTime;
    private int encounterWaveCount, encounterEnemyCount;
    private WaveConfiguration[] activeWaveConfigurations;
    private readonly HashSet<Behaviour> adoptedWaveSources = new HashSet<Behaviour>();
    private bool HasCustomWaves => useWaves && waveConfigurations != null && waveConfigurations.Length > 0;
    public int CurrentWave { get; private set; }
    public int TotalWaveCount => IsLocked && useWaves ? encounterWaveCount : HasCustomWaves ? waveConfigurations.Length : useWaves ? Mathf.Max(1, totalWaves) : 1;
    public bool IsWaitingForWave => waitingForWave;
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
            // 지역 이탈/리스폰은 전멸 판정보다 먼저 처리합니다.
            if (!inside) { CancelEncounter(); return; }
            remaining.RemoveWhere(IsDefeated);
            if (remaining.Count == 0)
            {
                if (!useWaves || CurrentWave >= encounterWaveCount) { CompleteEncounter(); return; }
                if (!waitingForWave)
                {
                    waitingForWave = true;
                    nextWaveTime = Time.time + Mathf.Max(0f, waveDelay);
                }
                if (Time.time >= nextWaveTime) StartNextWave();
            }
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
        foreach (var enemy in enemies) if (!IsDefeated(enemy)) remaining.Add(enemy);
        if (HasCustomWaves)
        {
            if (!TryCaptureWaveConfigurations()) { remaining.Clear(); return; }
            foreach (var wave in activeWaveConfigurations)
                foreach (var group in wave.groups)
                {
                    if (group.rangedSpawner != null) sources.Add(group.rangedSpawner);
                    else meleeSources.Add(group.meleeSpawner);
                }
        }
        else
        {
            activeWaveConfigurations = null;
            foreach (var source in spawners) if (source != null) sources.Add(source);
            foreach (var source in meleeSpawners) if (source != null) meleeSources.Add(source);
        }
        if (collectEnemiesInArea)
        {
            Collect<EnemyController>(); Collect<MeleeEnemy>(); Collect<RangedEnemy>(); Collect<TutorialBoss>();
            Collect<TriangleMeleeMonster>(); Collect<RangedEnemyController>();
            foreach (var source in FindObjectsByType<RangedEnemySpawner>(FindObjectsSortMode.None))
                if (!HasCustomWaves && source.enabled && entryArea.OverlapPoint(source.transform.position)) sources.Add(source);
            foreach (var source in FindObjectsByType<MeleeEnemySpawner>(FindObjectsSortMode.None))
                if (!HasCustomWaves && source.enabled && entryArea.OverlapPoint(source.transform.position)) meleeSources.Add(source);
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
        if (useWaves)
        {
            BeginWaveEncounter(sources, meleeSources);
            return;
        }
        foreach (var source in sources)
        {
            controlledSpawners.Add(source, source.enabled);
            source.enabled = false;
            var enemy = source.SpawnForCombatArea(player.transform);
            if (enemy != null) remaining.Add(enemy);
        }
        foreach (var source in meleeSources)
        {
            controlledSpawners.Add(source, source.enabled);
            source.enabled = false;
            var enemy = source.SpawnForCombatArea(player.transform);
            if (enemy != null) remaining.Add(enemy);
        }
        if (remaining.Count == 0) { CompleteEncounter(); return; }
        IsLocked = true;
        SetBarriers(true);
        onLocked.Invoke();
    }

    private bool TryCaptureWaveConfigurations()
    {
        var snapshot = new WaveConfiguration[waveConfigurations.Length];
        for (int w = 0; w < waveConfigurations.Length; w++)
        {
            var wave = waveConfigurations[w];
            if (wave == null || wave.groups == null || wave.groups.Length == 0)
            {
                Warn($"CombatAreaLock: {w + 1}웨이브의 Groups에 몬스터 구성을 넣으세요. 빈 웨이브는 시작하지 않습니다.");
                return false;
            }
            var groups = new WaveGroup[wave.groups.Length];
            for (int g = 0; g < wave.groups.Length; g++)
            {
                var group = wave.groups[g];
                if (group == null || group.count < 1 || (group.rangedSpawner == null) == (group.meleeSpawner == null))
                {
                    Warn($"CombatAreaLock: {w + 1}웨이브의 {g + 1}번 그룹에 스포너 한 종류만 지정하고 Count를 1 이상으로 설정하세요.");
                    return false;
                }
                var source = group.Source;
                bool validPrefab = source is RangedEnemySpawner r ? r.CanSpawnWave : ((MeleeEnemySpawner)source).CanSpawnWave;
                if (!source.gameObject.scene.IsValid() || !source.gameObject.activeInHierarchy || !validPrefab)
                {
                    Warn($"CombatAreaLock: {w + 1}웨이브의 {g + 1}번 스포너를 확인하세요. 유효한 프리팹을 가진 씬의 활성 스포너가 필요합니다.");
                    return false;
                }
                groups[g] = new WaveGroup { rangedSpawner = group.rangedSpawner, meleeSpawner = group.meleeSpawner, count = group.count };
            }
            snapshot[w] = new WaveConfiguration
            {
                waveName = wave.waveName, groups = groups,
                spawnPoints = wave.spawnPoints != null ? (Transform[])wave.spawnPoints.Clone() : new Transform[0]
            };
        }
        // 전투 중 Inspector를 수정해도 진행 중인 구성은 유지하며 다음 입장부터 반영합니다.
        activeWaveConfigurations = snapshot;
        return true;
    }

    private bool SpawnConfiguredWave()
    {
        var wave = activeWaveConfigurations[CurrentWave - 1];
        var points = wave.spawnPoints.Length > 0 ? wave.spawnPoints : waveSpawnPoints;
        int slot = 0;
        foreach (var group in wave.groups)
        {
            var source = group.Source;
            for (int i = 0; i < group.count; i++, slot++)
            {
                // 첫 웨이브 이전에 이미 생성된 동일 종류는 한 번만 인원에 포함합니다.
                if (CurrentWave == 1 && adoptedWaveSources.Remove(source)) continue;
                Vector3? position = null;
                if (points != null && points.Length > 0 && points[slot % points.Length] != null)
                    position = points[slot % points.Length].position;
                var enemy = source is RangedEnemySpawner r
                    ? r.SpawnForCombatWave(player.transform, position)
                    : ((MeleeEnemySpawner)source).SpawnForCombatWave(player.transform, position);
                if (enemy == null)
                {
                    Warn($"CombatAreaLock: {CurrentWave}웨이브 생성에 실패해 전투를 취소합니다.");
                    CancelEncounter(); return false;
                }
                remaining.Add(enemy); waveOwnedEnemies.Add(enemy);
            }
        }
        return true;
    }

    private void BeginWaveEncounter(HashSet<RangedEnemySpawner> ranged, HashSet<MeleeEnemySpawner> melee)
    {
        waveSources.Clear();
        foreach (var source in ranged) waveSources.Add(source);
        foreach (var source in melee) waveSources.Add(source);
        if (waveSources.Count == 0 || !AreWaveSourcesValid())
        {
            Warn("CombatAreaLock: 웨이브에는 유효한 몬스터 프리팹을 가진 근접/원거리 스포너가 필요합니다. 벽을 닫지 않습니다.");
            remaining.Clear(); waveSources.Clear(); return;
        }

        encounterWaveCount = activeWaveConfigurations != null ? activeWaveConfigurations.Length : Mathf.Max(1, totalWaves);
        encounterEnemyCount = Mathf.Max(1, enemiesPerWave);
        CurrentWave = 1;
        waitingForWave = false;
        var existing = new HashSet<GameObject>();
        adoptedWaveSources.Clear();
        foreach (var source in waveSources)
        {
            controlledSpawners.Add(source, source.enabled);
            source.enabled = false;
            var enemy = source is RangedEnemySpawner r ? r.SpawnedEnemy : ((MeleeEnemySpawner)source).SpawnedEnemy;
            if (enemy == null) continue;
            existing.Add(enemy);
            adoptedWaveSources.Add(source);
            remaining.Add(enemy);
            // 스포너가 만든 개체만 소유합니다. 씬에 직접 배치한 적은 삭제하지 않습니다.
            waveOwnedEnemies.Add(enemy);
        }
        if (!SpawnWaveEnemies(existing.Count)) return;
        IsLocked = true;
        SetBarriers(true);
        onLocked.Invoke();
    }

    private bool AreWaveSourcesValid()
    {
        foreach (var source in waveSources)
        {
            if (source == null || !source.gameObject.activeInHierarchy) return false;
            if (source is RangedEnemySpawner r ? !r.CanSpawnWave : !((MeleeEnemySpawner)source).CanSpawnWave) return false;
        }
        return true;
    }

    private void StartNextWave()
    {
        if (!AreWaveSourcesValid())
        {
            Warn("CombatAreaLock: 전투 중 스포너가 사라지거나 프리팹 연결이 없어져 웨이브를 취소합니다.");
            CancelEncounter(); return;
        }
        waitingForWave = false;
        CurrentWave++;
        SpawnWaveEnemies(0);
    }

    private bool SpawnWaveEnemies(int existingCount)
    {
        if (activeWaveConfigurations != null) return SpawnConfiguredWave();
        for (int i = existingCount; i < encounterEnemyCount; i++)
        {
            var source = waveSources[i % waveSources.Count];
            Vector3? position = null;
            if (waveSpawnPoints != null && waveSpawnPoints.Length > 0)
            {
                var point = waveSpawnPoints[i % waveSpawnPoints.Length];
                if (point != null) position = point.position;
            }
            var enemy = source is RangedEnemySpawner r
                ? r.SpawnForCombatWave(player.transform, position)
                : ((MeleeEnemySpawner)source).SpawnForCombatWave(player.transform, position);
            if (enemy == null)
            {
                Warn("CombatAreaLock: 웨이브 몬스터 생성에 실패해 문을 열고 전투를 취소합니다. 스포너와 플레이어 연결을 확인하세요.");
                CancelEncounter(); return false;
            }
            remaining.Add(enemy);
            waveOwnedEnemies.Add(enemy);
        }
        return true;
    }

    private void Collect<T>() where T : Component
    {
        foreach (var enemy in FindObjectsByType<T>(FindObjectsSortMode.None))
            if (entryArea.OverlapPoint(enemy.transform.position) && !IsDefeated(enemy.gameObject)) remaining.Add(enemy.gameObject);
    }

    private static bool IsDefeated(GameObject enemy)
        => MeleeEnemySpawner.IsDefeated(enemy) || RangedEnemySpawner.IsDefeated(enemy);

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
        waitingForWave = false;
        if (useWaves)
            foreach (var source in waveSources)
            {
                if (source is RangedEnemySpawner r && r != null) r.MarkCombatAreaCleared();
                else if (source is MeleeEnemySpawner m && m != null) m.MarkCombatAreaCleared();
            }
        waveOwnedEnemies.Clear(); waveSources.Clear();
        activeWaveConfigurations = null; adoptedWaveSources.Clear();
        remaining.Clear(); SetBarriers(false); RestoreSpawners();
        onCleared.Invoke();
    }

    private void CancelEncounter()
    {
        IsLocked = false; wasInside = false;
        waitingForWave = false; CurrentWave = 0;
        // 재입장 시 1웨이브부터 시작합니다. 이 전투에서 스포너가 만든 개체만 정리합니다.
        foreach (var enemy in waveOwnedEnemies)
            if (enemy != null) { enemy.SetActive(false); Destroy(enemy); }
        waveOwnedEnemies.Clear(); waveSources.Clear();
        activeWaveConfigurations = null; adoptedWaveSources.Clear();
        remaining.Clear(); SetBarriers(false); RestoreSpawners();
    }

    private void OnDisable() { activeAreas.Remove(this); CancelEncounter(); }
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
