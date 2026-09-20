using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RangedEnemy의 Kinematic Rigidbody 설정과 플레이어 몸 Collider의 충돌 제외/복구를 담당합니다.
/// 공격의 피해 판정은 제외하지 않습니다. 직접 부착하지 않는 partial 구현입니다.
/// </summary>
public sealed partial class RangedEnemy
{
    private readonly List<Collider2D> ignoredPlayerColliders = new List<Collider2D>();
    private Transform collisionTarget;

    private void ConfigureBody()
    {
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.interpolation = RigidbodyInterpolation2D.None;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.useFullKinematicContacts = true;
        bodyCollider.isTrigger = false;
    }

    private void RefreshPlayerCollisions()
    {
        if (target == collisionTarget) return;

        RestorePlayerCollisions();
        collisionTarget = target;
        if (target == null || bodyCollider == null) return;
        foreach (var other in target.GetComponentsInChildren<Collider2D>())
        {
            if (other.isTrigger || !other.enabled || Physics2D.GetIgnoreCollision(bodyCollider, other)) continue;
            ignoredPlayerColliders.Add(other);
            Physics2D.IgnoreCollision(bodyCollider, other, true);
        }
    }

    private void RestorePlayerCollisions()
    {
        foreach (var other in ignoredPlayerColliders)
        {
            if (bodyCollider != null && other != null) Physics2D.IgnoreCollision(bodyCollider, other, false);
        }
        ignoredPlayerColliders.Clear();
        collisionTarget = null;
    }
}
