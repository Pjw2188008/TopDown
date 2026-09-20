using UnityEngine;

/// <summary>
/// 플레이어 이동 후 카메라를 지연 없이 같은 위치 + 오프셋으로 맞춥니다.
/// 추적에 사용할 카메라 GameObject에 직접 부착합니다.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("카메라 추적")]
    [Tooltip("카메라가 따라갈 플레이어의 Transform입니다. 비어 있으면 카메라가 이동하지 않습니다.")]
    [SerializeField] private Transform player;

    [Tooltip("플레이어를 기준으로 유지할 카메라 위치 차이입니다. 2D에서는 보통 Z 값을 -10으로 유지합니다.")]
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);

    private void LateUpdate()
    {
        if (player == null)
        {
            return;
        }

        // 이동/대쉬가 끝난 같은 프레임에 바로 맞춥니다. 보간이나 시간 배율로 추적이 늦어지지 않습니다.
        transform.position = player.position + offset;
    }
}
