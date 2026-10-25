using System.Collections.Generic;
using UnityEngine;

/// <summary>사이드뷰 몬스터의 좌우 이동·중력·시야 검사입니다. 직접 부착하지 않습니다.</summary>
[System.Serializable]
public sealed class EnemyNavigation
{
    public EnemySideView sideView = new EnemySideView();
    private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();
    public void ApplyGravity(Transform root,Transform target,float dt,LayerMask mask) => sideView.TickGravity(root,target,dt,mask);
    public bool HasSight(Transform root, Transform target, Vector2 goal, LayerMask mask)
    {
        Vector2 start = root.position, delta = goal - start;
        if (delta.sqrMagnitude < .000001f) return true;
        var filter = new ContactFilter2D(); filter.SetLayerMask(mask.value & Physics2D.GetLayerCollisionMask(root.gameObject.layer)); filter.useTriggers = false;
        Physics2D.SyncTransforms();
        Physics2D.Raycast(start, delta.normalized, filter, hits, delta.magnitude);
        foreach (var hit in hits)
            if (hit.collider != null && !hit.collider.transform.IsChildOf(root)
                && (target == null || !hit.collider.transform.IsChildOf(target))) return false;
        return true;
    }


    public Vector2 Step(Transform root, Transform target, Vector2 goal, float distance, LayerMask mask)
    {
        if (distance <= 0f) return Vector2.zero;
        float x = Mathf.Clamp(goal.x-root.position.x,-distance,distance);
        return sideView.ConstrainStep(root,target,new Vector2(x,0f),mask);
    }
}
