using UnityEngine;

/// <summary>
/// 플레이어를 추적하며, 현재 맵 구역 안에 카메라 화면 전체를 제한합니다.
/// 카메라 GameObject에 부착합니다. 구역이 하나도 없으면 기존 자유 추적을 유지합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public partial class CameraFollow : MonoBehaviour
{
    [Header("카메라 추적")]
    [Tooltip("카메라가 따라갈 플레이어입니다. 비어 있으면 카메라가 이동하지 않습니다.")]
    [SerializeField] private Transform player;
    [Tooltip("플레이어를 기준으로 유지할 위치 차이입니다. 2D에서는 보통 Z=-10입니다.")]
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);

    [Header("구역별 카메라 경계")]
    [Tooltip("플레이어가 들어간 CameraBoundsArea 안에 화면 전체를 제한합니다. XY 평면을 보는 Orthographic 카메라용입니다.")]
    [SerializeField] private bool useAreaBounds = true;
    [Tooltip("방 안에서 플레이어 중심을 부드럽게 추적합니다. 작은 움직임은 Dead Zone 안에서 허용하고, 화면은 항상 바운드 안에 제한합니다. 끄면 기존 즉시 추적입니다.")]
    [SerializeField] private bool smoothAreaFollow = false;
    [Tooltip("카메라가 따라붙는 시간입니다. 낮을수록 빠르게 반응합니다. 0이면 즉시 추적합니다.")]
    [Min(0f), SerializeField] private float followSmoothTime = .12f;
    [Tooltip("카메라 중심에서 허용할 작은 움직임의 반경(X/Y, 월드 단위)입니다. 0이면 작은 움직임도 추적합니다.")]
    [SerializeField] private Vector2 followDeadZone = new Vector2(.25f, .15f);
    [Tooltip("한 프레임에 이 거리 이상 이동하면 리스폰/순간이동으로 보고 즉시 맞춥니다. 0이면 거리 검사를 끕니다. 방 전환은 암전 중에 별도로 맞춥니다.")]
    [Min(0f), SerializeField] private float teleportSnapDistance = 6f;
    [Tooltip("시작 위치가 어떤 구역에도 포함되지 않을 때 사용할 선택적 기본 구역입니다.")]
    [SerializeField] private CameraBoundsArea defaultArea;
    [Tooltip("구역 사이의 빈 공간으로 이동해도 마지막 구역의 경계를 유지합니다. 다음 구역에 들어가면 자동 전환합니다.")]
    [SerializeField] private bool keepLastAreaOutside = true;
    [Tooltip("구역이 화면보다 작으면 Orthographic Size를 줄여 화면 전체가 들어가게 합니다. 큰 구역에서는 원래 크기로 복구합니다. 끄면 작은 축의 중심에 고정되지만 구역 밖이 보일 수 있습니다.")]
    [SerializeField] private bool fitSmallAreas = true;

    private Camera viewCamera;
    private CameraBoundsArea activeArea;
    private float requestedSize;
    private float lastAppliedSize;
    private bool managingZoom;
    private bool warnedUnsupported;
    private bool hasFollowPosition;
    private Vector3 followVelocity;
    private Vector3 lastFollowTarget;
    private CameraBoundsArea followedArea;
    private Transform followedPlayer;

    public CameraBoundsArea ActiveArea => activeArea;

    private void OnEnable()
    {
        viewCamera = GetComponent<Camera>();
        activeArea = null;
        managingZoom = false;
        warnedUnsupported = false;
        ResetRoomTransition();
        ResetAreaFollow();
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            ResetRoomTransition();
            ResetAreaFollow();
            activeArea = null;
            RestoreZoom();
            return;
        }

        Vector3 targetPosition = player.TryGetComponent<PlayerMove>(out var playerMove)
            ? playerMove.GroundPosition : player.position;
        Vector3 desired = targetPosition + offset;
        if (!useAreaBounds || viewCamera == null)
        {
            ResetAreaFollow();
            ResetRoomTransition();
            activeArea = null;
            RestoreZoom();
            transform.position = desired;
            return;
        }

        var enteredArea = CameraBoundsArea.FindAt(targetPosition, activeArea);
        var nextArea = enteredArea;
        if (nextArea == null)
            nextArea = keepLastAreaOutside && activeArea != null && activeArea.IsUsable
                ? activeArea : (defaultArea != null && defaultArea.IsUsable ? defaultArea : null);

        bool canFade = fadeBetweenAreas && (!fadeOnlyInSideView || (playerMove != null && playerMove.IsSideView));
        UpdateRoomTransition(nextArea, canFade, Time.unscaledDeltaTime);
        // 암전 중에는 이전 화면을 고정하고, 완전히 검어진 프레임에서만 경계/줌을 바꿉니다.
        if (roomFadePhase == RoomFadePhase.FadeOut) return;

        if (activeArea == null)
        {
            ResetAreaFollow();
            RestoreZoom();
            transform.position = desired;
            return;
        }

        // 화면 반경은 XY 평면 기준입니다. Z축 회전은 지원하고 기울어진/원근 카메라는 제외합니다.
        if (!viewCamera.orthographic || Mathf.Abs(Vector3.Dot(transform.forward, Vector3.forward)) < .9999f)
        {
            ResetAreaFollow();
            if (!warnedUnsupported)
            {
                Debug.LogWarning("카메라 구역 경계는 XY 평면을 보는 Orthographic 카메라에서 사용하세요.", this);
                warnedUnsupported = true;
            }
            RestoreZoom();
            transform.position = desired;
            return;
        }

        if (!managingZoom || !Mathf.Approximately(viewCamera.orthographicSize, lastAppliedSize))
            requestedSize = viewCamera.orthographicSize; // 실행 중 외부 줌 변경도 새 기준으로 반영합니다.

        float aspect = Mathf.Max(.0001f, viewCamera.aspect);
        Vector3 right = transform.right;
        Vector3 up = transform.up;
        Vector2 unitHalfView = new Vector2(Mathf.Abs(right.x) * aspect + Mathf.Abs(up.x),
            Mathf.Abs(right.y) * aspect + Mathf.Abs(up.y));
        Bounds bounds = activeArea.WorldBounds;
        float size = fitSmallAreas
            ? CameraBoundsMath.FitOrthographicSize(requestedSize, bounds, unitHalfView)
            : requestedSize;
        viewCamera.orthographicSize = size;
        lastAppliedSize = size;
        managingZoom = true;
        transform.position = GetBoundedFollowPosition(desired, bounds, unitHalfView * size,
            Time.unscaledDeltaTime, roomFadeAlpha >= 1f);
    }

    private Vector3 GetBoundedFollowPosition(Vector3 desired, Bounds bounds, Vector2 halfView, float dt, bool forceSnap)
    {
        bool snap = forceSnap || !hasFollowPosition || followedArea != activeArea || followedPlayer != player
            || (teleportSnapDistance > 0f && (desired - lastFollowTarget).sqrMagnitude > teleportSnapDistance * teleportSnapDistance);
        hasFollowPosition = true;
        followedArea = activeArea;
        followedPlayer = player;
        lastFollowTarget = desired;
        Vector3 target = CameraBoundsMath.ClampPosition(desired, bounds, halfView);
        if (!smoothAreaFollow || followSmoothTime <= 0f || snap)
        {
            followVelocity = Vector3.zero;
            return target;
        }

        Vector3 current = CameraBoundsMath.ClampPosition(transform.position, bounds, halfView);
        Vector3 delta = desired - current;
        float deadX = Mathf.Max(0f, followDeadZone.x), deadY = Mathf.Max(0f, followDeadZone.y);
        if (Mathf.Abs(delta.x) <= deadX) followVelocity.x = 0f;
        if (Mathf.Abs(delta.y) <= deadY) followVelocity.y = 0f;
        target = new Vector3(current.x + Mathf.Sign(delta.x) * Mathf.Max(0f, Mathf.Abs(delta.x) - deadX),
            current.y + Mathf.Sign(delta.y) * Mathf.Max(0f, Mathf.Abs(delta.y) - deadY), desired.z);
        target = CameraBoundsMath.ClampPosition(target, bounds, halfView);
        if (dt <= 0f)
        {
            current.z = desired.z;
            return current;
        }
        Vector3 next = Vector3.SmoothDamp(current, target, ref followVelocity,
            followSmoothTime, Mathf.Infinity, Mathf.Max(0f, dt));
        next.z = desired.z;
        Vector3 clamped = CameraBoundsMath.ClampPosition(next, bounds, halfView);
        if (!Mathf.Approximately(clamped.x, next.x)) followVelocity.x = 0f;
        if (!Mathf.Approximately(clamped.y, next.y)) followVelocity.y = 0f;
        followVelocity.z = 0f;
        return clamped;
    }

    private void ResetAreaFollow()
    {
        hasFollowPosition = false;
        followVelocity = Vector3.zero;
        followedArea = null;
        followedPlayer = null;
    }

    private void RestoreZoom()
    {
        if (managingZoom && viewCamera != null && Mathf.Approximately(viewCamera.orthographicSize, lastAppliedSize))
            viewCamera.orthographicSize = requestedSize;
        managingZoom = false;
    }

    private void OnDisable()
    {
        ResetAreaFollow();
        ResetRoomTransition();
        RestoreZoom();
        activeArea = null;
    }

    private void OnDestroy() => DestroyRoomFadeOverlay();
}
