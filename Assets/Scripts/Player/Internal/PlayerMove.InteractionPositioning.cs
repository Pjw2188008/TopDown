using UnityEngine;

/// <summary>물체 잡기 간격과 미는 방향에 맞춘 자리 이동입니다. PlayerMove partial이므로 직접 부착하지 않습니다.</summary>
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

    private bool TryRepositionForInteraction(float deltaTime, out bool moved)
    {
        moved = false;
        if (!IsMovingObject) return false;
        Physics2D.SyncTransforms();
        if (!TryGetInteractionBounds(transform, out Bounds playerBounds)
            || !TryGetInteractionBounds(heldInteractable.transform, out Bounds objectBounds)) return false;

        Vector2 start = playerBounds.center;
        Vector2 center = objectBounds.center;
        Vector2 bodyExtents = playerBounds.extents + objectBounds.extents;
        Vector2 outer = bodyExtents + Vector2.one * Mathf.Max(.04f, interactionGrabGap);
        Vector2 destination = center - Vector2.Scale(interactionFacing, outer);
        if ((destination - start).sqrMagnitude <= .000001f) return true;

        interactionRouteNodes[0] = start;
        interactionRouteNodes[1] = destination;
        interactionRouteNodes[2] = center + new Vector2(-outer.x, -outer.y);
        interactionRouteNodes[3] = center + new Vector2(-outer.x, outer.y);
        interactionRouteNodes[4] = center + new Vector2(outer.x, outer.y);
        interactionRouteNodes[5] = center + new Vector2(outer.x, -outer.y);
        for (int i = 0; i < interactionRouteNodes.Length; i++)
        {
            interactionRouteCosts[i] = float.PositiveInfinity;
            interactionRoutePrevious[i] = -1;
            interactionRouteVisited[i] = false;
        }
        interactionRouteCosts[0] = 0f;

        // 양쪽 모서리 경로 중 현재 충돌 검사로 통과 가능한 짧은 경로를 선택합니다.
        // 물체의 충돌 레이어 설정과 무관하게 물체 내부를 가로지르는 경로는 제외합니다.
        Bounds forbidden = new Bounds(new Vector3(center.x, center.y, 0f),
            new Vector3(bodyExtents.x * 2f, bodyExtents.y * 2f, 2f));
        for (int pass = 0; pass < interactionRouteNodes.Length; pass++)
        {
            int nearest = -1;
            for (int i = 0; i < interactionRouteNodes.Length; i++)
                if (!interactionRouteVisited[i] && !float.IsPositiveInfinity(interactionRouteCosts[i])
                    && (nearest < 0 || interactionRouteCosts[i] < interactionRouteCosts[nearest])) nearest = i;
            if (nearest < 0 || nearest == 1) break;
            interactionRouteVisited[nearest] = true;
            for (int next = 1; next < interactionRouteNodes.Length; next++)
            {
                if (interactionRouteVisited[next] || next == nearest) continue;
                Vector2 from = interactionRouteNodes[nearest];
                Vector2 delta = interactionRouteNodes[next] - from;
                float distance = delta.magnitude;
                float cost = interactionRouteCosts[nearest] + distance;
                if (cost >= interactionRouteCosts[next]) continue;
                if (distance > .0001f)
                {
                    if (forbidden.IntersectRay(new Ray((Vector3)from, (Vector3)(delta / distance)), out float entry)
                        && entry <= distance) continue;
                    float clear = InteractionMotion.AllowedDistance(transform, null, delta / distance, distance,
                        interactionBlockingLayers, interactionHits, from - start);
                    if (clear + .0001f < distance) continue;
                }
                interactionRouteCosts[next] = cost;
                interactionRoutePrevious[next] = nearest;
            }
        }
        if (interactionRoutePrevious[1] < 0) return false; // 막히면 이동/밀기를 멈추고 F로 놓을 수 있습니다.
        int waypoint = 1;
        while (interactionRoutePrevious[waypoint] > 0) waypoint = interactionRoutePrevious[waypoint];
        Vector2 step = interactionRouteNodes[waypoint] - start;
        float wanted = Mathf.Min(step.magnitude, Mathf.Max(0f, moveSpeed) * Mathf.Max(0f, deltaTime));
        if (wanted <= .00001f) return false;
        float allowed = InteractionMotion.AllowedDistance(transform, null, step.normalized, wanted,
            interactionBlockingLayers, interactionHits);
        ApplyPlayerDisplacement(step.normalized * allowed);
        Physics2D.SyncTransforms();
        moved = allowed > .00001f;
        return false;
    }
}
