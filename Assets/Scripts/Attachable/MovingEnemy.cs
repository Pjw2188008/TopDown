using UnityEngine;

/// <summary>
/// 시작 위치를 중심으로 지정한 축을 왕복 순찰하고, 가속 오류의 속도 배율을 적용받습니다.
/// 움직이게 만들 적 또는 테스트 대상 GameObject에 직접 부착합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MovingEnemy : MonoBehaviour, IAccelerationTarget
{
    [Header("이동 적 순찰")]
    [Tooltip("가속 오류가 없을 때 사용하는 기본 이동 속도입니다. 가속 적용 중에는 이 값에 가속 배율이 곱해집니다.")]
    [SerializeField, Min(0f)] private float baseSpeed = 1.2f;

    [Tooltip("시작 위치에서 각 방향의 순찰 끝점까지 거리입니다. 실제 이동 구간은 이 값의 두 배입니다.")]
    [SerializeField, Min(0.1f)] private float patrolDistance = 2f;

    [Tooltip("순찰 축의 방향입니다. (1, 0)은 좌우, (0, 1)은 위아래 이동이며, (0, 0)이면 자동으로 좌우 이동합니다.")]
    [SerializeField] private Vector2 patrolAxis = Vector2.right;

    private Vector3 startPosition;
    private Vector3 patrolDirection;
    private int directionSign = 1;
    private float accelerationMultiplier = 1f;

    public float CurrentSpeed => baseSpeed * accelerationMultiplier;

    private void Awake()
    {
        startPosition = transform.position;
        patrolDirection = patrolAxis.sqrMagnitude > 0f
            ? ((Vector3)patrolAxis).normalized
            : Vector3.right;
    }

    private void Update()
    {
        Vector3 targetPosition = startPosition + patrolDirection * patrolDistance * directionSign;
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            CurrentSpeed * Time.deltaTime);

        if ((transform.position - targetPosition).sqrMagnitude <= 0.0001f)
        {
            directionSign *= -1;
        }
    }

    public void SetAccelerationMultiplier(float multiplier)
    {
        accelerationMultiplier = Mathf.Max(1f, multiplier);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = Application.isPlaying ? startPosition : transform.position;
        Vector3 direction = patrolAxis.sqrMagnitude > 0f
            ? ((Vector3)patrolAxis).normalized
            : Vector3.right;

        Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.9f);
        Gizmos.DrawLine(origin - direction * patrolDistance, origin + direction * patrolDistance);
        Gizmos.DrawWireSphere(origin - direction * patrolDistance, 0.12f);
        Gizmos.DrawWireSphere(origin + direction * patrolDistance, 0.12f);
    }
}
