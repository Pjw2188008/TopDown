using UnityEngine;

/// <summary>
/// 플레이어를 지연 없이 추적하며, 현재 맵 구역 안에 카메라 화면 전체를 제한합니다.
/// 카메라 GameObject에 부착합니다. 구역이 하나도 없으면 기존 자유 추적을 유지합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("카메라 추적")]
    [Tooltip("카메라가 따라갈 플레이어입니다. 비어 있으면 카메라가 이동하지 않습니다.")]
    [SerializeField] private Transform player;
    [Tooltip("플레이어를 기준으로 유지할 위치 차이입니다. 2D에서는 보통 Z=-10입니다.")]
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);

    [Header("구역별 카메라 경계")]
    [Tooltip("플레이어가 들어간 CameraBoundsArea 안에 화면 전체를 제한합니다. XY 평면을 보는 Orthographic 카메라용입니다.")]
    [SerializeField] private bool useAreaBounds = true;
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

    public CameraBoundsArea ActiveArea => activeArea;

    private void OnEnable()
    {
        viewCamera = GetComponent<Camera>();
        activeArea = null;
        managingZoom = false;
        warnedUnsupported = false;
    }

    private void LateUpdate()
    {
        if (player == null) return;

        Vector3 desired = player.position + offset;
        if (!useAreaBounds || viewCamera == null)
        {
            activeArea = null;
            RestoreZoom();
            transform.position = desired;
            return;
        }

        var enteredArea = CameraBoundsArea.FindAt(player.position, activeArea);
        if (enteredArea != null) activeArea = enteredArea;
        else if (!keepLastAreaOutside || activeArea == null || !activeArea.IsUsable)
            activeArea = defaultArea != null && defaultArea.IsUsable ? defaultArea : null;

        if (activeArea == null)
        {
            RestoreZoom();
            transform.position = desired;
            return;
        }

        // 화면 반경은 XY 평면 기준입니다. Z축 회전은 지원하고 기울어진/원근 카메라는 제외합니다.
        if (!viewCamera.orthographic || Mathf.Abs(Vector3.Dot(transform.forward, Vector3.forward)) < .9999f)
        {
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
        transform.position = CameraBoundsMath.ClampPosition(desired, bounds, unitHalfView * size);
    }

    private void RestoreZoom()
    {
        if (managingZoom && viewCamera != null && Mathf.Approximately(viewCamera.orthographicSize, lastAppliedSize))
            viewCamera.orthographicSize = requestedSize;
        managingZoom = false;
    }

    private void OnDisable()
    {
        RestoreZoom();
        activeArea = null;
    }
}
