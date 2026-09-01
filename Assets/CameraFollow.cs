using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float smoothSpeed = 5f;
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
