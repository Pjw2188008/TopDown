using System.Collections.Generic;
using UnityEngine;

/// <summary>2D 사이드뷰 대상에게만 적용하는 바닥 낙석/좌우 충격파/발 고정 확대입니다. 직접 부착하지 않습니다.</summary>
public sealed partial class TutorialBoss
{
    [Header("사이드뷰 패턴")]
    [SerializeField, Tooltip("사이드뷰 낙석이 착지할 바닥/발판 레이어입니다. 일반 Collider2D가 필요하며 Trigger와 몬스터/플레이어는 제외합니다.")]
    private LayerMask sideViewGroundLayers = Physics2D.DefaultRaycastLayers;
    private readonly List<RaycastHit2D> groundHits = new List<RaycastHit2D>();
    private bool IsSideViewBattle => sight.IsSideView(transform,player != null ? player.transform : null);

    private Bounds RockfallWorldBounds
    {
        get
        {
            var area = rockfallArea != null ? rockfallArea : roomArea;
            if (area != null) return area.bounds;
            return new Bounds(roomOrigin+roomOffset,new Vector3(Mathf.Max(.1f,roomSize.x),Mathf.Max(.1f,roomSize.y),1f));
        }
    }

    private bool TrySideViewRockPoint(float x,float radius,bool targetPlayer,out Vector2 point)
    {
        point = default;
        var area = rockfallArea != null ? rockfallArea : roomArea;
        if (area != null && (!area.enabled || !area.gameObject.activeInHierarchy)) return false;
        var bounds = RockfallWorldBounds;
        if (x-radius < bounds.min.x || x+radius > bounds.max.x) return false;
        float top = bounds.max.y;
        if (targetPlayer && player != null && EnemySideView.TryBounds(player.transform,out var playerBounds))
            top = Mathf.Min(top,playerBounds.min.y+.08f);
        if (top <= bounds.min.y) return false;
        var filter = new ContactFilter2D();filter.SetLayerMask(sideViewGroundLayers);filter.useTriggers = false;
        Physics2D.Raycast(new Vector2(x,top),Vector2.down,filter,groundHits,top-bounds.min.y);
        foreach(var hit in groundHits)
        {
            var shape = hit.collider;
            if (shape == null || hit.normal.y < .9f || shape.transform.IsChildOf(transform)
                || shape.GetComponentInParent<PlayerMove>() != null || shape.GetComponentInParent<ICombatDamageable>() != null) continue;
            point = new Vector2(x,hit.point.y+radius+.02f);
            // 바닥에 원을 묻지 않고 올립니다. 높이가 다른 플랫폼은 실제 윗면을 따릅니다.
            if (!IsRockPointValid(point,radius)) return false;
            return true;
        }
        return false;
    }

    private void SetPatternBodyScale(Vector3 scale)
    {
        Physics2D.SyncTransforms();
        bool anchorFeet = IsSideViewBattle && EnemySideView.TryBounds(transform,out _);
        float feet = 0f;
        if (anchorFeet && EnemySideView.TryBounds(transform,out var before)) feet = before.min.y;
        transform.localScale = scale;
        Physics2D.SyncTransforms();
        if (anchorFeet && EnemySideView.TryBounds(transform,out var after))
            EnemySideView.Move(transform,Vector2.up*(feet-after.min.y));
    }

    private void FitRockVisuals(FallingRock rock)
    {
        float radius = rock.radius;
        if (rock.sideView)
        {
            rock.point.y = rock.surfaceY+radius+.02f;
            if (rock.root != null) rock.root.transform.position = new Vector3(rock.point.x,rock.point.y,transform.position.z);
            if (rock.warning != null)
            {
                Fit(rock.warning,new Vector2(radius*2f,.1f));
                rock.warning.transform.localPosition = Vector3.down*radius;
            }
            if (rock.picture != null) Fit(rock.picture,Vector2.one*radius*2f);
        }
        else
        {
            if (rock.warning != null) Fit(rock.warning,Vector2.one*radius*2f);
            if (rock.picture != null) Fit(rock.picture,Vector2.one*radius*1.5f);
        }
    }
}
