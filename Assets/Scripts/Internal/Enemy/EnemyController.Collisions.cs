using System.Collections.Generic;
using UnityEngine;

/// <summary>EnemyController의 플레이어 몸 충돌 제외 부분입니다. partial 구현이므로 직접 부착하지 않습니다.</summary>
public partial class EnemyController
{
    private void RefreshBodyCollisions()
    {
        if (collisionTarget != target || !ignorePlayerBodyCollision) RestoreBodyCollisions();
        collisionTarget = target;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        ownColliderBuffer.Clear(); playerColliderBuffer.Clear();
        GetComponentsInChildren(false, ownColliderBuffer);
        if (ignorePlayerBodyCollision && target != null)
            target.GetComponentsInChildren(false, playerColliderBuffer);
        foreach (Collider2D own in ownColliderBuffer)
        {
            if (autoAssignEnemyLayer && enemyLayer >= 0 && own.gameObject.layer == 0) own.gameObject.layer = enemyLayer;
            if (!ignorePlayerBodyCollision || target == null || !own.enabled || own.isTrigger) continue;
            foreach (Collider2D other in playerColliderBuffer)
            {
                if (!other.enabled || other.isTrigger || Physics2D.GetIgnoreCollision(own, other)) continue;
                if (!IsCollisionPairTracked(own, other))
                    ignoredPairs.Add(new CollisionPair { enemy = own, player = other });
                Physics2D.IgnoreCollision(own, other, true);
            }
        }
    }

    private bool IsCollisionPairTracked(Collider2D own, Collider2D other)
    {
        // 프레임마다 람다 캡처 객체를 만들지 않으면서 기존 충돌 제외 소유권을 유지합니다.
        for (int i = 0; i < ignoredPairs.Count; i++)
            if (ignoredPairs[i].enemy == own && ignoredPairs[i].player == other) return true;
        return false;
    }

    private void RestoreBodyCollisions()
    {
        foreach (CollisionPair pair in ignoredPairs)
            if (pair.enemy != null && pair.player != null) Physics2D.IgnoreCollision(pair.enemy, pair.player, false);
        ignoredPairs.Clear(); collisionTarget = null;
    }
}
