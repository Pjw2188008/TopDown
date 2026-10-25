using UnityEngine;

/// <summary>사이드뷰 물체 잡기의 수평 간격 보정입니다. PlayerMove partial이므로 직접 부착하지 않습니다.</summary>
public partial class PlayerMove
{
    private void EnsureInteractionGrabGap()
    {
        if (!IsMovingObject || interactionGrabGap <= 0f) return;
        Physics2D.SyncTransforms();
        if (!TryGetInteractionBounds(transform, out Bounds playerBounds)
            || !TryGetInteractionBounds(heldInteractable.transform, out Bounds objectBounds)) return;

        // Collider의 월드 경계를 기준으로 계산합니다. 큰 물체/거대화와 중심 오프셋도 반영합니다.
        Vector2 offset = objectBounds.center - playerBounds.center;
        offset.y = 0f;
        Vector2 direction = offset.sqrMagnitude > .0001f ? offset.normalized
            : (lastDirection.sqrMagnitude > .0001f ? lastDirection.normalized : Vector2.right);
        Vector2 separation = (Vector2)(playerBounds.extents + objectBounds.extents) + Vector2.one * interactionGrabGap;
        float xDistance = Mathf.Abs(direction.x) > .0001f ? separation.x / Mathf.Abs(direction.x) : float.PositiveInfinity;
        float yDistance = Mathf.Abs(direction.y) > .0001f ? separation.y / Mathf.Abs(direction.y) : float.PositiveInfinity;
        float wanted = Mathf.Max(0f, Mathf.Min(xDistance, yDistance) - offset.magnitude);
        if (wanted <= .0001f) return;

        // 플레이어는 그대로 두고 물체만 밀어냅니다. 벽/장애물까지의 안전 거리로 제한합니다.
        float allowed = InteractionMotion.AllowedDistance(heldInteractable.transform, transform,
            direction, wanted, interactionBlockingLayers, interactionHits);
        if (allowed <= 0f) return;
        heldInteractable.MoveBy(transform, direction * allowed);
        Physics2D.SyncTransforms();
    }

    private static bool TryGetInteractionBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        var colliders = UnityEngine.Pool.ListPool<Collider2D>.Get();
        try
        {
            root.GetComponentsInChildren(false, colliders);
            foreach (Collider2D shape in colliders)
            {
                if (!shape.enabled || shape.isTrigger || !shape.gameObject.activeInHierarchy
                    || (shape.attachedRigidbody != null && !shape.attachedRigidbody.simulated)) continue;
                if (!found) bounds = shape.bounds;
                else bounds.Encapsulate(shape.bounds);
                found = true;
            }
            return found;
        }
        finally { UnityEngine.Pool.ListPool<Collider2D>.Release(colliders); }
    }

}
