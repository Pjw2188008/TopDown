using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 닿으면 즉사하는 2D 레이저입니다. 사물이 빔 어디든 가로막으면 빔 전체가 안전해집니다.
/// Inspector에 직접 연결한 BoxCollider2D로 판정합니다. 콜라이더를 자동 생성/검색/수정하지 않습니다.
/// </summary>
[DefaultExecutionOrder(-100)] // 플레이어 Update 이동 이후, CameraFollow LateUpdate 이전에 리스폰합니다.
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed partial class LaserHazard : MonoBehaviour
{
    [Header("직접 지정할 판정")]
    [Tooltip("직접 만든 BoxCollider2D를 연결하세요. 비어 있으면 판정하지 않습니다. 같은 오브젝트 또는 자식에 두고 Is Trigger를 켜세요. Size/Offset/Enabled/Transform은 스크립트가 변경하지 않습니다.")]
    [SerializeField] private BoxCollider2D beamCollider;
    [Header("차단")]
    [Tooltip("사물 차단을 검사할 레이어입니다. 이 레이어 안의 LaserBlocker/MovableInteractable/PasteTarget(Object)을 인식합니다.")]
    [SerializeField] private LayerMask blockerLayers = Physics2D.DefaultRaycastLayers;
    [Tooltip("마지막 차단 사물이 빠진 후 다시 위험해지기까지의 게임 시간(초)입니다.")]
    [SerializeField, Min(0f)] private float reactivationDelay = .3f;
    [Header("플레이어")]
    [Tooltip("즉사 판정을 받을 플레이어입니다. 비우면 활성 PlayerMove를 자동 검색합니다.")]
    [SerializeField] private PlayerMove player;
    [Tooltip("플레이어가 이 레이저로 처음 즉사했을 때 호출됩니다. 게임오버 UI 등을 연결할 수 있습니다.")]
    [SerializeField] private UnityEvent onPlayerKilled = new UnityEvent();
    private BoxCollider2D trackedBeamCollider;
    private readonly List<Collider2D> overlaps = new List<Collider2D>(16);
    private readonly List<RaycastHit2D> sweepHits = new List<RaycastHit2D>(16);
    private readonly List<RaycastHit2D> blockerSweepHits = new List<RaycastHit2D>(16);
    private Collider2D[] playerShapes;
    private PlayerMove trackedPlayer;
    private Vector2 previousPlayerPosition;
    private bool hasPreviousPosition;
    private float nextPlayerSearch;
    private float safeUntil;
    private bool wasBlocked;
    private int observedTeleportVersion;

    public bool IsBlocked { get; private set; }
    public bool IsLethal { get; private set; }

    private void Awake()
    {
        InitializeVerticalMotion();
        ResolveBeamRenderer();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying) ReleaseBeamVisibility();
        hasPreviousPosition = false;
        wasBlocked = false;
        safeUntil = 0f;
    }

    private void Start()
    {
        FindPlayer();
        if (player == null) return;
        previousPlayerPosition = player.transform.position;
        observedTeleportVersion = player.HazardTeleportVersion;
        hasPreviousPosition = true;
    }

    private void LateUpdate()
    {
        AdvanceVerticalMotion(Time.deltaTime);
        bool validBeam = CanUseBeamCollider();
        if (trackedBeamCollider != beamCollider || !validBeam) hasPreviousPosition = false;
        trackedBeamCollider = beamCollider;
        Physics2D.SyncTransforms();

        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(blockerLayers);
        overlaps.Clear();
        if (validBeam) beamCollider.Overlap(filter, overlaps);
        IsBlocked = false;
        foreach (var shape in overlaps)
        {
            if (IsBlockingObject(shape)) { IsBlocked = true; break; }
        }
        // 빠르게 움직이는 빔이 한 프레임 안에 사물을 지나가도 차단/재가동 대기 시간을 적용합니다.
        if (validBeam && !IsBlocked && beamFrameDisplacement.sqrMagnitude > .000001f)
        {
            blockerSweepHits.Clear();
            // 현재 지정 콜라이더를 이동 반대 방향으로 검사합니다. 새 판정 상자를 만들지 않습니다.
            beamCollider.Cast(-beamFrameDisplacement.normalized, filter, blockerSweepHits, beamFrameDisplacement.magnitude, false);
            foreach (var sweptHit in blockerSweepHits)
            {
                if (IsBlockingObject(sweptHit.collider)) { IsBlocked = true; break; }
            }
        }
        if (IsBlocked) safeUntil = Time.time + Mathf.Max(0f, reactivationDelay);
        else if (wasBlocked) safeUntil = Time.time + Mathf.Max(0f, reactivationDelay);
        wasBlocked = IsBlocked;
        IsLethal = validBeam && !IsBlocked && Time.time >= safeUntil;
        UpdateBeamVisibility();

        FindPlayer();
        if (player == null) return;
        if (observedTeleportVersion != player.HazardTeleportVersion) hasPreviousPosition = false;
        Vector2 currentPosition = player.transform.position;
        if (Time.timeScale > 0f && IsLethal && player.isActiveAndEnabled && !player.IsInstantlyDead && TouchesPlayer(currentPosition))
        {
            if (player.KillInstantly(gameObject)) onPlayerKilled.Invoke();
        }
        previousPlayerPosition = player.transform.position;
        observedTeleportVersion = player.HazardTeleportVersion;
        hasPreviousPosition = validBeam;
    }

    private bool IsBlockingObject(Collider2D shape)
    {
        if (shape == null || shape.transform.IsChildOf(transform) || shape.GetComponentInParent<PlayerMove>() != null) return false;
        var marker = shape.GetComponentInParent<LaserBlocker>();
        if (marker != null) return marker.BlocksLaser; // 명시적 표식을 우선합니다.
        var movable = shape.GetComponentInParent<MovableInteractable>();
        if (movable != null && movable.isActiveAndEnabled) return true;
        var target = shape.GetComponentInParent<PasteTarget>();
        return target != null && target.isActiveAndEnabled && target.TargetType == PasteTargetType.Object;
    }

    private void FindPlayer()
    {
        if (player == null && Time.unscaledTime >= nextPlayerSearch)
        {
            nextPlayerSearch = Time.unscaledTime + .5f;
            player = FindFirstObjectByType<PlayerMove>();
        }
        if (trackedPlayer == player) return;
        trackedPlayer = player;
        playerShapes = player != null ? player.GetComponentsInChildren<Collider2D>() : null;
        hasPreviousPosition = false;
    }

    private bool TouchesPlayer(Vector2 currentPosition)
    {
        if (playerShapes == null) return false;
        // 플레이어 이동과 빔의 평행 이동을 함께 고려한 상대 이동 경로입니다.
        Vector2 rewind = previousPlayerPosition - currentPosition + beamFrameDisplacement;
        var filter = new ContactFilter2D { useTriggers = true };
        foreach (var shape in playerShapes)
        {
            if (shape == null || !shape.enabled || shape.isTrigger || !shape.gameObject.activeInHierarchy) continue;
            if (shape.Distance(beamCollider).isOverlapped) return true;
            // 프레임 사이에 가느다란 빔을 대시로 통과해도 실제 플레이어 Collider의 이동 경로로 잡습니다.
            if (!hasPreviousPosition || rewind.sqrMagnitude <= .000001f) continue;
            sweepHits.Clear();
            shape.Cast(rewind.normalized, filter, sweepHits, rewind.magnitude);
            foreach (var hit in sweepHits) if (hit.collider == beamCollider) return true;
        }
        return false;
    }

    /// <summary>외부 워프/리스폰은 이동 경로가 아니므로 호출하여 이전 위치 기록을 초기화합니다.</summary>
    public void ResetPlayerTracking() => hasPreviousPosition = false;

    private void OnDisable()
    {
        IsBlocked = false; IsLethal = false; hasPreviousPosition = false;
        if (Application.isPlaying) HideBeam();
        else ReleaseBeamVisibility();
    }

    private void OnDestroy()
    {
        ReleaseBeamVisibility();
    }

    private void OnDrawGizmos()
    {
        Color old = Gizmos.color;
        Gizmos.color = Color.red;
        DrawBeamOutline(Vector3.zero);
        DrawVerticalMotionGizmos();
        Gizmos.color = old;
    }
}
