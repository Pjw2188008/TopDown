using UnityEngine;

/// <summary>
/// LaserHazard의 월드 Y축 왕복 이동과 이동 범위 기즈모입니다.
/// 본체와 같은 partial 클래스이므로 별도로 부착하지 않습니다.
/// </summary>
public sealed partial class LaserHazard
{
    [Header("상하 왕복 이동")]
    [Tooltip("켜면 처음 배치한 위치를 기준으로 월드 Y축을 따라 위아래로 왕복합니다. 끄면 현재 위치에서 멈춥니다.")]
    [SerializeField] private bool moveVertically = true;
    [Tooltip("최초 배치 위치에서 위쪽으로 이동할 최대 거리입니다(월드 단위).")]
    [SerializeField, Min(0f)] private float upperTravel = 2f;
    [Tooltip("최초 배치 위치에서 아래쪽으로 이동할 최대 거리입니다(월드 단위).")]
    [SerializeField, Min(0f)] private float lowerTravel = 2f;
    [Tooltip("초당 상하 이동 거리입니다. 0이면 멈춥니다. 편집 모드 슬로모션과 일시 정지를 따릅니다.")]
    [SerializeField, Min(0f)] private float verticalSpeed = 1f;
    [Tooltip("처음 위쪽으로 움직일지 결정합니다. 끄면 아래쪽으로 시작합니다.")]
    [SerializeField] private bool startMovingUp = true;
    [Tooltip("Scene에서 위/아래 한계 빔과 이동 범위를 표시합니다.")]
    [SerializeField] private bool showMovementGizmo = true;

    private Vector3 verticalOrigin;
    private bool verticalMotionInitialized;
    private bool movingUp;
    private float verticalOffset;
    private Vector2 beamFrameDisplacement;

    private void InitializeVerticalMotion()
    {
        verticalOrigin = transform.position;
        verticalOffset = 0f;
        movingUp = startMovingUp;
        verticalMotionInitialized = true;
    }

    private void AdvanceVerticalMotion(float deltaTime)
    {
        beamFrameDisplacement = Vector2.zero;
        if (!verticalMotionInitialized) InitializeVerticalMotion();
        if (!moveVertically || verticalSpeed <= 0f || deltaTime <= 0f) return;

        float top = Mathf.Max(0f, upperTravel);
        float bottom = -Mathf.Max(0f, lowerTravel);
        float target = movingUp ? top : bottom;
        float next = Mathf.Clamp(Mathf.MoveTowards(verticalOffset, target, verticalSpeed * deltaTime), bottom, top);
        float delta = next - verticalOffset;
        verticalOffset = next;
        Vector3 position = transform.position;
        position.y = verticalOrigin.y + verticalOffset;
        transform.position = position;
        beamFrameDisplacement = Vector2.up * delta;
        // 한 프레임에 여러 번 왕복하지 않아 이동 경로/차단 판정이 누락되지 않습니다.
        if (Mathf.Approximately(verticalOffset, target)) movingUp = !movingUp;
    }

    private void DrawVerticalMotionGizmos()
    {
        if (!moveVertically || !showMovementGizmo) return;
        float offset = Application.isPlaying && verticalMotionInitialized ? verticalOffset : 0f;
        Vector3 start = transform.position - Vector3.up * offset;
        Vector3 top = Vector3.up * Mathf.Max(0f, upperTravel);
        Vector3 bottom = Vector3.down * Mathf.Max(0f, lowerTravel);
        Gizmos.color = Color.cyan;
        DrawBeamOutline(top - Vector3.up * offset);
        DrawBeamOutline(bottom - Vector3.up * offset);
        Gizmos.DrawLine(start + bottom, start + top);
        Gizmos.DrawWireSphere(start + top, .12f);
        Gizmos.DrawWireSphere(start + bottom, .12f);
    }
}
