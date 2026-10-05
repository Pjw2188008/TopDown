using System.Collections.Generic;
using UnityEngine;

/// <summary>이동형 적이 공유하는 런타임 2D 우회 경로 탐색입니다. 직접 부착하지 않습니다.
/// 실제 몸 Collider를 Cast하므로 몸보다 좁은 통로/대각 모서리를 통과하지 않습니다.
/// 베이크 없이 제한된 주변 영역을 A*로 탐색하고 움직이는 장애물에 맞춰 재탐색합니다.</summary>
[System.Serializable]
public sealed class EnemyNavigation
{
    [Tooltip("길찾기 격자 간격(월드 단위). 작으면 좁은 길을 더 잘 찾지만 계산량이 증가합니다.")]
    [Min(.15f)] public float cellSize = .5f;
    [Tooltip("한 번에 탐색할 주변 반경입니다. 이보다 큰 복잡한 미로는 보장하지 않습니다.")]
    [Min(2f)] public float searchRadius = 10f;
    [Tooltip("경로 재탐색 간격입니다. 매 프레임 전체 경로를 계산하지 않습니다.")]
    [Min(.1f)] public float repathInterval = .5f;
    [Tooltip("한 번의 탐색에서 검사할 최대 격자 수입니다. 몬스터가 많으면 낮춰 계산량을 제한하세요.")]
    [Range(64, 2048)] public int maxSearchNodes = 512;
    private readonly List<Vector2> path = new List<Vector2>();
    private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();
    private float nextSearch;
    private Vector2 lastGoal;
    private int waypoint;
    private struct Node { public Vector2Int cell; public float cost, score; }
    private readonly List<Node> open = new List<Node>();
    private readonly Dictionary<Vector2Int, float> costs = new Dictionary<Vector2Int, float>();
    private readonly Dictionary<Vector2Int, Vector2Int> parents = new Dictionary<Vector2Int, Vector2Int>();
    private readonly HashSet<Vector2Int> closed = new HashSet<Vector2Int>();

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

    private bool Clear(Transform root, Transform target, Vector2 from, Vector2 to, LayerMask mask)
    {
        Vector2 delta = to - from; float length = delta.magnitude;
        if (length < .0001f) return true;
        return InteractionMotion.AllowedDistance(root, target, delta / length, length, mask, hits,
            from - (Vector2)root.position) >= length - .001f;
    }

    public Vector2 Step(Transform root, Transform target, Vector2 goal, float distance, LayerMask mask)
    {
        if (distance <= 0f) return Vector2.zero;
        Physics2D.SyncTransforms();
        Vector2 start = root.position;
        if (Clear(root, target, start, goal, mask))
        {
            path.Clear(); waypoint = 0;
            return Vector2.ClampMagnitude(goal - start, distance);
        }
        float spacing = Mathf.Max(.15f, cellSize);
        if (Time.time >= nextSearch || (goal - lastGoal).sqrMagnitude > spacing * spacing * 4)
        {
            Build(root, target, start, goal, mask, spacing);
            lastGoal = goal; nextSearch = Time.time + Mathf.Max(.1f, repathInterval);
        }
        while (waypoint < path.Count && Vector2.Distance(start, path[waypoint]) < .03f) waypoint++;
        if (waypoint >= path.Count) return Vector2.zero;
        // 새 장애물이 경로를 막으면 밀고 들어가지 않고 다음 재탐색을 기다립니다.
        Vector2 step = Vector2.ClampMagnitude(path[waypoint] - start, distance);
        return Clear(root, target, start, start + step, mask) ? step : Vector2.zero;
    }

    private void Build(Transform root, Transform target, Vector2 origin, Vector2 goal, LayerMask mask, float spacing)
    {
        path.Clear(); waypoint = 0; open.Clear(); costs.Clear(); parents.Clear(); closed.Clear();
        var zero = Vector2Int.zero;
        float initial = Vector2.Distance(origin, goal), bestDistance = initial;
        Vector2Int best = zero; bool reached = false;
        costs[zero] = 0; open.Add(new Node { cell = zero, score = initial });
        int radius = Mathf.CeilToInt(Mathf.Max(2f, searchRadius) / spacing);
        int budget = Mathf.Clamp(maxSearchNodes, 64, 2048);
        while (open.Count > 0 && closed.Count < budget)
        {
            int index = 0;
            for (int i = 1; i < open.Count; i++) if (open[i].score < open[index].score) index = i;
            Node current = open[index]; open.RemoveAt(index);
            if (!closed.Add(current.cell)) continue;
            Vector2 position = origin + (Vector2)current.cell * spacing;
            float remaining = Vector2.Distance(position, goal);
            if (remaining < bestDistance) { bestDistance = remaining; best = current.cell; }
            if (remaining <= spacing * 1.5f && Clear(root, target, position, goal, mask))
            { best = current.cell; reached = true; break; }
            for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                var cell = current.cell + new Vector2Int(x, y);
                if (Mathf.Abs(cell.x) > radius || Mathf.Abs(cell.y) > radius || closed.Contains(cell)) continue;
                float cost = current.cost + spacing * (x != 0 && y != 0 ? 1.414214f : 1f);
                if (costs.TryGetValue(cell, out float old) && old <= cost) continue;
                Vector2 next = origin + (Vector2)cell * spacing;
                if (!Clear(root, target, position, next, mask)) continue;
                costs[cell] = cost; parents[cell] = current.cell;
                open.Add(new Node { cell = cell, cost = cost, score = cost + Vector2.Distance(next, goal) });
            }
        }
        if (best == zero && !reached) return; // 닫힌 구역은 순간이동하거나 벽을 뚫지 않습니다.
        for (var cell = best; cell != zero; cell = parents[cell]) path.Add(origin + (Vector2)cell * spacing);
        path.Reverse();
        if (reached) path.Add(goal);
    }
}
