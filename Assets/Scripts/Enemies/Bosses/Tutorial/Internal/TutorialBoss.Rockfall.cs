using System.Collections.Generic;
using UnityEngine;

/// <summary>첫 맵 패턴: 고정된 여러 위치에 경고 후 낙석. TutorialBoss의 partial이며 별도 부착하지 않습니다.</summary>
public sealed partial class TutorialBoss
{
    [Header("맵 패턴 1 / 낙석")]
    [SerializeField, Min(0f), Tooltip("방 입장 후 첫 낙석까지의 게임 시간입니다. 진행 중인 근접 공격과 후딜은 끝까지 기다립니다.")] private float rockfallFirstDelay = 3f;
    [SerializeField, Min(.1f), Tooltip("낙석 패턴이 끝난 후 다음 낙석까지의 최소 간격입니다. 거리에 관계없이 발동합니다.")] private float rockfallInterval = 6f;
    [SerializeField, Range(1,16), Tooltip("한 패턴의 최대 낙석 수입니다. 안전한 배치 공간이 부족하면 줄어듭니다.")] private int rockfallCount = 5;
    [SerializeField, Tooltip("낙석 전용 BoxCollider2D 범위입니다(Is Trigger 권장). 비우면 Room Area 또는 Room Size를 사용합니다. 반드시 보스 방 안에 배치하세요.")] private BoxCollider2D rockfallArea;
    [SerializeField, Tooltip("한 지점을 발동 당시 플레이어 위치에 배치합니다. 경고 이후에는 추적하지 않으며 벽/경계에 걸리면 무작위 위치로 대체합니다.")] private bool rockfallTargetPlayer = true;
    [SerializeField, Min(.1f), Tooltip("돌이 내려오기 전 바닥 경고를 보여주는 시간입니다.")] private float rockfallWarningTime = 1f;
    [SerializeField, Min(.05f), Tooltip("돌이 위에서 지면까지 떨어지는 시간입니다. 이 시간이 끝날 때만 한 번 피해를 판정합니다.")] private float rockfallFallTime = .35f;
    [SerializeField, Min(0f), Tooltip("각 돌의 낙하 시작 간격입니다. 0이면 동시에 떨어집니다. 모든 위치는 패턴 시작 시 미리 표시됩니다.")] private float rockfallStagger = .15f;
    [SerializeField, Min(.1f), Tooltip("낙하 연출의 시작 높이입니다. 판정은 고정된 바닥 위치에만 있습니다.")] private float rockfallHeight = 4f;
    [SerializeField, Min(.1f), Tooltip("바닥 경고 원과 실제 타격 반지름입니다(월드 단위). 위치끼리는 겹치지 않도록 선택합니다.")] private float rockfallRadius = .65f;
    [SerializeField, Min(0f), Tooltip("돌 1개당 착지 피해입니다. 패링 횟수에는 포함되지 않으며 일반 가드/반사 오류의 피해 처리는 유지합니다.")] private float rockfallDamage = 2f;
    [SerializeField, UnityEngine.Animations.NotKeyable, Tooltip("나중에 넣을 돌 이미지입니다. 비우면 회색 사각형으로 표시합니다. 중앙 피벗 이미지를 권장합니다.")] private Sprite rockfallSprite;
    [SerializeField, Tooltip("경고와 돌의 Sorting Layer 이름입니다. 비우면 보스 SpriteRenderer의 레이어를 사용합니다.")] private string rockfallSortingLayer = "";
    [SerializeField, Tooltip("바닥 경고의 정렬 순서입니다. 돌은 이 값보다 1 높게 그립니다. 맵에 가려지면 올리세요.")] private int rockfallSortingOrder = 25;

    private sealed class FallingRock
    {
        public Vector2 point;
        public float radius, delay, elapsed;
        public bool impacted;
        public GameObject root;
        public SpriteRenderer warning, picture;
    }
    private readonly List<FallingRock> fallingRocks = new List<FallingRock>();
    private float rockfallRemaining;
    private const float RockImpactHold = .18f;

    private void BeginRockfall()
    {
        ClearRockfall();
        Physics2D.SyncTransforms();
        int count = Mathf.Clamp(rockfallCount,1,16);
        float radius = Mathf.Max(.1f,rockfallRadius);
        for (int i=0;i<count;i++)
        {
            Vector2 point = player.transform.position;
            bool found = i == 0 && rockfallTargetPlayer && IsRockPointValid(point,radius);
            for (int attempt=0;!found && attempt<60;attempt++)
            { point = RandomRockPoint();found = IsRockPointValid(point,radius); }
            if (!found) continue; // 좁은 방에서도 무한 탐색하거나 경고를 강제로 겹치지 않습니다.
            fallingRocks.Add(CreateRock(point,radius,fallingRocks.Count*Mathf.Max(0f,rockfallStagger)));
        }
        if (fallingRocks.Count == 0)
        {
            rockfallRemaining = Mathf.Max(.1f,rockfallInterval);
            SetPhase(Phase.Recovery,attackCooldown);return;
        }
        SetPhase(Phase.Rockfall,0f);
    }

    private Vector2 RandomRockPoint()
    {
        var area = rockfallArea != null ? rockfallArea : roomArea;
        if (area != null)
        {
            Vector2 local = area.offset + new Vector2(Random.Range(-.5f,.5f)*area.size.x,Random.Range(-.5f,.5f)*area.size.y);
            return area.transform.TransformPoint(local);
        }
        return roomOrigin + roomOffset + new Vector2(Random.Range(-.5f,.5f)*Mathf.Max(.1f,roomSize.x),Random.Range(-.5f,.5f)*Mathf.Max(.1f,roomSize.y));
    }

    private bool IsRockPointValid(Vector2 point,float radius)
    {
        // 원을 감싸는 네 모서리도 검사하므로 회전된 Box 영역에서도 경계를 넘지 않습니다.
        for (int x=-1;x<=1;x+=2) for (int y=-1;y<=1;y+=2)
        {
            Vector2 corner = point + new Vector2(x,y)*radius;
            if (!IsInsideRoom(corner)) return false;
            if (rockfallArea != null && (!rockfallArea.enabled || !rockfallArea.gameObject.activeInHierarchy
                || !rockfallArea.OverlapPoint(corner))) return false;
        }
        foreach (var rock in fallingRocks)
            if (Vector2.Distance(point,rock.point) < radius+rock.radius+.25f) return false;
        foreach (var hit in Physics2D.OverlapCircleAll(point,radius,blockingLayers))
        {
            if (hit.isTrigger || hit.transform.IsChildOf(transform) || hit.GetComponentInParent<PlayerMove>() != null
                || hit.GetComponentInParent<ICombatDamageable>() != null || hit.GetComponentInParent<ReflectProjectile>() != null) continue;
            return false;
        }
        return true;
    }

    private FallingRock CreateRock(Vector2 point,float radius,float delay)
    {
        var root = new GameObject("Boss Rockfall");root.transform.position = new Vector3(point.x,point.y,transform.position.z);
        int layer = string.IsNullOrEmpty(rockfallSortingLayer)
            ? (bodyVisual != null ? bodyVisual.sortingLayerID : 0) : SortingLayer.NameToID(rockfallSortingLayer);
        var warning = new GameObject("Landing Warning").AddComponent<SpriteRenderer>();warning.transform.SetParent(root.transform,false);
        warning.sprite = circleSprite;warning.color = new Color(1f,.35f,.05f,.35f);
        warning.sortingLayerID = layer;warning.sortingOrder = rockfallSortingOrder;
        Fit(warning,Vector2.one*radius*2f);
        var picture = new GameObject("Falling Rock").AddComponent<SpriteRenderer>();picture.transform.SetParent(root.transform,false);
        picture.sprite = rockfallSprite != null ? rockfallSprite : squareSprite;
        picture.color = rockfallSprite != null ? Color.white : new Color(.4f,.38f,.35f);
        picture.sortingLayerID = layer;picture.sortingOrder = rockfallSortingOrder+1;
        Fit(picture,Vector2.one*radius*1.5f);picture.enabled = false;
        return new FallingRock { point=point,radius=radius,delay=delay,root=root,warning=warning,picture=picture };
    }

    private void TickRockfall(float dt)
    {
        bool finished = true;
        float warningTime = Mathf.Max(.1f,rockfallWarningTime),fallTime = Mathf.Max(.05f,rockfallFallTime);
        foreach (var rock in fallingRocks)
        {
            rock.elapsed += dt;
            float fallProgress = (rock.elapsed-rock.delay-warningTime)/fallTime;
            if (rock.warning != null)
                rock.warning.color = new Color(1f,.35f,.05f,Mathf.Lerp(.35f,.7f,Mathf.Clamp01((rock.elapsed-rock.delay)/warningTime)));
            if (fallProgress >= 0f && rock.picture != null)
            {
                rock.picture.enabled = true;
                float t = Mathf.Clamp01(fallProgress);
                rock.picture.transform.localPosition = Vector3.up * (Mathf.Max(.1f,rockfallHeight)*(1f-t*t));
            }
            if (!rock.impacted && fallProgress >= 1f)
            {
                rock.impacted = true; // 피해 콜백에서 상태가 바뀌더라도 중복 타격하지 않습니다.
                if (rock.warning != null) rock.warning.enabled = false;
                ApplyRockDamage(rock);
                // 반사 피해로 보스가 죽거나 경직되어 목록을 정리했다면 바로 종료합니다.
                if (phase != Phase.Rockfall) return;
                if (!IsPlayerInRoom()) { ClearRockfall();SetPhase(Phase.Recovery,attackCooldown);return; }
            }
            bool done = rock.elapsed >= rock.delay+warningTime+fallTime+RockImpactHold;
            if (done && rock.root != null) rock.root.SetActive(false);
            finished &= done;
        }
        if (!finished) return;
        ClearRockfall();rockfallRemaining = Mathf.Max(.1f,rockfallInterval);
        SetPhase(Phase.Recovery,attackCooldown);
    }

    private void ApplyRockDamage(FallingRock rock)
    {
        if (!IsPlayerInRoom()) return;
        Physics2D.SyncTransforms();
        foreach (var hit in Physics2D.OverlapCircleAll(rock.point,rock.radius))
        {
            if (!hit.enabled || hit.isTrigger || hit.GetComponentInParent<PlayerMove>() != player) continue;
            player.ReceiveDamage(Mathf.Max(0f,rockfallDamage),gameObject,true);
            break; // 플레이어 Collider가 여러 개여도 돌 하나당 피해는 한 번입니다.
        }
    }

    private void ClearRockfall()
    {
        foreach (var rock in fallingRocks)
            if (rock.root != null) { rock.root.SetActive(false);Destroy(rock.root); }
        fallingRocks.Clear();
    }

    private void DrawRockfallAreaGizmo()
    {
        if (rockfallArea == null) return;
        var previous = Gizmos.matrix;Gizmos.color = new Color(1f,.5f,.1f);
        Gizmos.matrix = rockfallArea.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(rockfallArea.offset,rockfallArea.size);Gizmos.matrix = previous;
    }
}
