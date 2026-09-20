using System.Collections.Generic;
using UnityEngine;

/// <summary>잡은 물체와 플레이어의 공통 이동 거리를 제한하는 충돌 검사입니다. 별도 부착하지 않습니다.</summary>
public static class InteractionMotion
{
    public static float AllowedDistance(Transform root, Transform partner, Vector2 direction, float wanted,
        LayerMask blockingLayers, List<RaycastHit2D> hits, Vector2 castOffset = default)
    {
        float allowed = wanted;
        var colliders = UnityEngine.Pool.ListPool<Collider2D>.Get();
        try
        {
            root.GetComponentsInChildren(false, colliders);
            foreach (Collider2D shape in colliders)
            {
                if (!shape.enabled || shape.isTrigger || !shape.gameObject.activeInHierarchy) continue;
                if (shape.attachedRigidbody != null && !shape.attachedRigidbody.simulated) continue;
                var filter = new ContactFilter2D();
                filter.SetLayerMask(blockingLayers.value & Physics2D.GetLayerCollisionMask(shape.gameObject.layer));
                filter.useTriggers = false;
                // A bounds cast also works for existing Transform-driven objects without a Rigidbody2D.
                // castOffset allows checking a route segment without moving the actual Transform.
                Vector2 castCenter = (Vector2)shape.bounds.center + castOffset;
                Physics2D.BoxCast(castCenter, shape.bounds.size, 0f, direction, filter, hits, wanted + 0.02f);
                foreach (RaycastHit2D hit in hits)
                {
                    if (hit.collider == null || hit.collider.transform.IsChildOf(root)
                        || (partner != null && hit.collider.transform.IsChildOf(partner))) continue;
                    if (Physics2D.GetIgnoreCollision(shape, hit.collider)) continue;
                    if (Vector2.Dot(direction, hit.normal) >= -0.001f) continue;
                    if (hit.distance == 0f && Vector2.Dot(direction, (Vector2)hit.collider.bounds.center - castCenter) < -0.001f)
                        continue; // Allow moving out of an existing overlap, never further into it.
                    allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - 0.02f));
                }
            }
            return allowed;
        }
        finally { UnityEngine.Pool.ListPool<Collider2D>.Release(colliders); }
    }
}
