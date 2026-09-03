using UnityEngine;

/// <summary>
/// 지정한 플레이어를 부드럽게 따라가도록 카메라 위치를 갱신합니다.
/// 추적에 사용할 카메라 GameObject에 직접 부착합니다.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("카메라 추적")]
    [Tooltip("카메라가 따라갈 플레이어의 Transform입니다. 비어 있으면 카메라가 이동하지 않습니다.")]
    [SerializeField] private Transform player;

    [Tooltip("플레이어를 따라가는 보간 속도입니다. 값이 높을수록 카메라가 더 빠르고 즉각적으로 따라갑니다.")]
    [SerializeField] private float smoothSpeed = 5f;

    [Tooltip("플레이어를 기준으로 유지할 카메라 위치 차이입니다. 2D에서는 보통 Z 값을 -10으로 유지합니다.")]
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f); // Z축 거리

    private void LateUpdate()
    {
        if (player == null)
        {
            return;
        }

        // 목표 위치 설정
        Vector3 targetPosition = player.position + offset;

        // Smooth 이동
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }
}
