using System.Collections.Generic;
using UnityEngine;

/// <summary>플레이어 물리 구성과 이동 전 충돌 검사입니다. PlayerMove partial이므로 직접 부착하지 않습니다.</summary>
public partial class PlayerMove
{
    [Header("플레이어 몸 충돌")]
    [Tooltip("걷기/달리기를 막는 레이어입니다. Trigger와 개별 IgnoreCollision 대상은 통과합니다. 대쉬/물체 옮기기는 각각의 기존 충돌 마스크를 사용합니다.")]
    [SerializeField] private LayerMask movementBlockingLayers = Physics2D.DefaultRaycastLayers;
    private Rigidbody2D playerBody;
    private readonly List<RaycastHit2D> movementHits = new List<RaycastHit2D>(16);

    private void Awake()
    {
        CaptureRespawnPosition();
        EnsureProjectileCollider();
        EnsurePlayerPhysics();
    }

    private void EnsurePlayerPhysics()
    {
        playerBody = GetComponent<Rigidbody2D>();
        if (playerBody == null) playerBody = gameObject.AddComponent<Rigidbody2D>();
        // 이 캐릭터는 힘으로 움직이지 않는 직접 조작형입니다. Dynamic 충돌 보정과
        // Transform 이동이 경쟁하지 않도록 Kinematic + 선행 Cast를 사용합니다.
        playerBody.bodyType = RigidbodyType2D.Kinematic;
        playerBody.gravityScale = 0f;
        playerBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        playerBody.interpolation = RigidbodyInterpolation2D.None;
        playerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        playerBody.useFullKinematicContacts = true;
        playerBody.simulated = true;
        playerBody.linearVelocity = Vector2.zero;
        playerBody.angularVelocity = 0f;
    }

    // 이미 충돌 거리를 검사한 대쉬/물체 옮기기도 같은 위치 적용 경로를 사용합니다.
    private void ApplyPlayerDisplacement(Vector2 displacement)
    {
        if (playerBody == null) EnsurePlayerPhysics();
        Vector3 nextPosition = transform.position + (Vector3)displacement;
        playerBody.position = nextPosition;
        // Rigidbody.position만 바꾸면 Transform 반영이 물리 스텝까지 지연될 수 있습니다.
        // Update 기반 조작/카메라/연속 Cast가 같은 좌표를 보도록 즉시 맞춥니다.
        transform.position = nextPosition;
        Physics2D.SyncTransforms();
    }

    private Vector2 MovePlayerWithCollision(Vector2 displacement)
    {
        Vector2 start = transform.position;
        // 축별로 검사하여 대각선 접근에서도 벽과의 접촉 여유를 유지합니다.
        // 막힌 축은 멈추고, 열린 축으로는 계속 이동합니다.
        if (displacement.x != 0f && displacement.y != 0f)
        {
            MovePlayerStep(new Vector2(displacement.x, 0f));
            MovePlayerStep(new Vector2(0f, displacement.y));
        }
        else MovePlayerStep(displacement);
        return (Vector2)transform.position - start;
    }

    private Vector2 MovePlayerStep(Vector2 displacement)
    {
        float wanted = displacement.magnitude;
        if (wanted <= .00001f) return Vector2.zero;
        Physics2D.SyncTransforms();
        Vector2 direction = displacement / wanted;
        float allowed = InteractionMotion.AllowedDistance(transform, null, direction, wanted,
            movementBlockingLayers, movementHits);
        Vector2 step = direction * allowed;
        ApplyPlayerDisplacement(step);
        return step;
    }
}
