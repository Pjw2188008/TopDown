using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RangedEnemy의 대상 검색, 접근 이동, 벽 충돌 이동과 좌우 반전을 담당합니다.
/// 하나의 RangedEnemy를 나눈 partial 구현이므로 직접 부착하지 않습니다.
/// </summary>
public sealed partial class RangedEnemy
{
    private static readonly int MovingHash = Animator.StringToHash("IsMoving");
    private readonly List<RaycastHit2D> moveHits = new List<RaycastHit2D>(16);
    private float speedMultiplier = 1f;
    private float nextTargetSearch;
    [SerializeField] private EnemyNavigation navigation = new EnemyNavigation();
    private bool CanSeeTarget() => target != null && navigation.HasSight(transform, target, target.position, movementBlockingLayers);

    private bool HasAnimator => animator != null && animator.runtimeAnimatorController != null;

    private void FindTarget()
    {
        if (target != null || Time.time < nextTargetSearch) return;

        nextTargetSearch = Time.time + .5f;
        var player = FindFirstObjectByType<PlayerMove>();
        if (player != null) target = player.transform;
    }

    private void MoveTowardsTarget(Vector2 difference, float distance, float deltaTime)
    {
        float desiredDistance = CanSeeTarget() ? Mathf.Min(Mathf.Max(0f, stoppingDistance), Mathf.Max(.1f, attackRange)) : 0f;
        Vector2 displacement = distance > desiredDistance && distance > .0001f
            ? navigation.Step(transform, target, target.position, Mathf.Min(CurrentMoveSpeed * deltaTime, distance - desiredDistance), movementBlockingLayers)
            : Vector2.zero;
        Vector2 start = transform.position;

        MoveAxis(displacement);
        Vector2 moved = (Vector2)transform.position - start;
        bool isMoving = moved.sqrMagnitude > .00000001f;
        if (isMoving) Face(moved);
        SetMoving(isMoving);
    }

    private void MoveAxis(Vector2 delta)
    {
        float distance = delta.magnitude;
        if (distance <= .00001f) return;

        Physics2D.SyncTransforms();
        float allowed = InteractionMotion.AllowedDistance(transform, target, delta / distance,
            distance, movementBlockingLayers, moveHits);
        Vector3 next = transform.position + (Vector3)(delta / distance * allowed);
        body.position = next;
        transform.position = next;
        Physics2D.SyncTransforms();
    }

    private void Face(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > .0001f) visual.flipX = direction.x < 0;
    }

    private void SetMoving(bool moving)
    {
        if (HasAnimator) animator.SetBool(MovingHash, moving);
    }
}
