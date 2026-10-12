using System.Collections.Generic;
using UnityEngine;

/// <summary>같은 씬의 2D 사이드뷰 플레이어를 상대할 때만 사용하는 중력/좌우 이동입니다. 직접 부착하지 않습니다.</summary>
[System.Serializable]
public sealed class EnemySideView
{
    [Tooltip("대상이 사이드뷰 ON / 2.5D OFF이면 좌우 추적과 중력을 사용합니다. 탑다운/2.5D에서는 기존 길찾기를 유지합니다.")]
    public bool followPlayerMode = true;
    [Min(0f), Tooltip("사이드뷰에서 초당 하강 가속도입니다. 공격/경직 중에도 바닥에 착지합니다.")]
    public float gravity = 30f;
    [Min(.1f)] public float maxFallSpeed = 20f;
    [Tooltip("발밑이 없는 방향으로 걷거나 찌르지 않습니다. 자동 점프/발판 간 길찾기는 하지 않습니다.")]
    public bool stopAtLedges = true;
    private float fallSpeed, nextSearch;
    private PlayerMove cachedPlayer;
    private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();

    public bool IsActive(Transform root, Transform target)
    {
        if (!followPlayerMode) return false;
        if (target != null) cachedPlayer = target.GetComponentInParent<PlayerMove>();
        else if (cachedPlayer == null || cachedPlayer.gameObject.scene != root.gameObject.scene)
        {
            if (Time.time < nextSearch) return false;
            nextSearch = Time.time + .5f;
            foreach (var candidate in Object.FindObjectsByType<PlayerMove>(FindObjectsSortMode.None))
                if (candidate.gameObject.scene == root.gameObject.scene) { cachedPlayer = candidate; break; }
        }
        return cachedPlayer != null && cachedPlayer.gameObject.scene == root.gameObject.scene
            && cachedPlayer.IsSideView && !cachedPlayer.IsSideViewDepthMovement;
    }

    public void TickGravity(Transform root, Transform target, float dt, LayerMask mask)
    {
        if (!IsActive(root,target)) { fallSpeed = 0f; return; }
        if (target == null && cachedPlayer != null) target = cachedPlayer.transform;
        if (dt <= 0f) return;
        // 기존 적의 Kinematic 이동과 같은 Cast를 사용해 물리 보정/Transform 이동이 싸우지 않게 합니다.
        if (root.TryGetComponent<Rigidbody2D>(out var body))
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        Physics2D.SyncTransforms();
        fallSpeed = Mathf.Min(Mathf.Max(.1f,maxFallSpeed),fallSpeed+Mathf.Max(0f,gravity)*dt);
        float wanted = fallSpeed*dt;
        float allowed = InteractionMotion.AllowedDistance(root,target,Vector2.down,wanted,mask,hits,default,true);
        Move(root,Vector2.down*allowed);
        if (allowed+.0001f < wanted) fallSpeed = 0f;
    }

    public Vector2 ConstrainStep(Transform root, Transform target, Vector2 step, LayerMask mask)
    {
        if (!IsActive(root,target)) return step;
        if (target == null && cachedPlayer != null) target = cachedPlayer.transform;
        step.y = 0f;
        if (Mathf.Abs(step.x) < .00001f) return Vector2.zero;
        Physics2D.SyncTransforms();
        float sign = Mathf.Sign(step.x), length = Mathf.Abs(step.x);
        length = InteractionMotion.AllowedDistance(root,target,Vector2.right*sign,length,mask,hits,default,true);
        if (stopAtLedges && TryBounds(root,out var bounds))
        {
            // 현재 공중에 있으면 낙하만 합니다. 큰 dt/전진 찌르기도 중간 낭떠러지를 건너뛰지 않습니다.
            if (!Supported(root,target,new Vector2(bounds.center.x,bounds.min.y+.04f),mask)) return Vector2.zero;
            float safe = 0f;
            for (float d = Mathf.Min(.1f,length); d > safe; d = Mathf.Min(safe+.1f,length))
            {
                var probe = new Vector2(bounds.center.x+sign*(bounds.extents.x+d),bounds.min.y+.04f);
                if (!Supported(root,target,probe,mask)) break;
                safe = d;
                if (safe >= length) break;
            }
            length = safe;
        }
        return Vector2.right*(sign*length);
    }

    private bool Supported(Transform root,Transform target,Vector2 point,LayerMask mask)
    {
        var filter = new ContactFilter2D();filter.SetLayerMask(mask.value & Physics2D.GetLayerCollisionMask(root.gameObject.layer));filter.useTriggers = false;
        Physics2D.Raycast(point,Vector2.down,filter,hits,.16f);
        foreach(var hit in hits)
            if (hit.collider != null && !hit.collider.transform.IsChildOf(root)
                && (target == null || !hit.collider.transform.IsChildOf(target)) && hit.normal.y >= .5f
                && hit.collider.GetComponentInParent<ICombatDamageable>() == null) return true;
        return false;
    }

    public static bool TryBounds(Transform root,out Bounds bounds)
    {
        bounds = default;bool found = false;
        foreach(var shape in root.GetComponentsInChildren<Collider2D>())
        {
            if (!shape.enabled || !shape.gameObject.activeInHierarchy) continue;
            if (!found) { bounds = shape.bounds;found = true; } else bounds.Encapsulate(shape.bounds);
        }
        return found;
    }

    public static void Move(Transform root,Vector2 displacement)
    {
        var next = root.position+(Vector3)displacement;
        if (root.TryGetComponent<Rigidbody2D>(out var body)) body.position = next;
        root.position = next;Physics2D.SyncTransforms();
    }
}
