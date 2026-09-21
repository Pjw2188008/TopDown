using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카메라가 보여줄 수 있는 맵 구역입니다. 빈 GameObject에 부착하고 Scene 핸들로 범위를 조절합니다.
/// 물리 Collider가 아니므로 플레이어 이동을 막지 않습니다. 월드 XY축에 정렬된 사각형입니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class CameraBoundsArea : MonoBehaviour
{
    private static readonly List<CameraBoundsArea> areas = new List<CameraBoundsArea>();

    [Tooltip("구역 중심의 로컬 오프셋입니다. 구역은 회전과 관계없이 월드 XY축에 정렬됩니다.")]
    [SerializeField] private Vector2 center;
    [Tooltip("구역의 가로/세로 크기입니다. Transform Scale을 반영하며 회전된 다각형은 지원하지 않습니다.")]
    [SerializeField] private Vector2 size = new Vector2(20f, 12f);
    [Tooltip("여러 구역이 겹치면 높은 우선순위가 선택됩니다. 같은 우선순위에서는 현재 구역을 유지합니다.")]
    [SerializeField] private int priority;
    [Tooltip("Scene 뷰에 표시할 경계선 색상입니다.")]
    [SerializeField] private Color gizmoColor = new Color(.2f, 1f, .7f, .9f);

    public int Priority => priority;
    public Bounds WorldBounds
    {
        get
        {
            Vector3 scale = transform.lossyScale;
            Vector3 worldSize = new Vector3(Mathf.Max(.01f, size.x) * Mathf.Abs(scale.x),
                Mathf.Max(.01f, size.y) * Mathf.Abs(scale.y), 0f);
            return new Bounds(transform.TransformPoint(center), worldSize);
        }
    }

    public bool IsUsable => isActiveAndEnabled && WorldBounds.size.x > .0001f && WorldBounds.size.y > .0001f;

    public bool Contains(Vector2 point)
    {
        Bounds bounds = WorldBounds;
        return IsUsable && point.x >= bounds.min.x && point.x <= bounds.max.x
            && point.y >= bounds.min.y && point.y <= bounds.max.y;
    }

    /// <summary>구역 진입은 플레이어 위치로 검사합니다. Rigidbody/Trigger 이벤트가 필요하지 않습니다.</summary>
    public static CameraBoundsArea FindAt(Vector2 point, CameraBoundsArea current)
    {
        CameraBoundsArea selected = current != null && current.Contains(point) ? current : null;
        foreach (var area in areas)
        {
            if (area == null || !area.Contains(point)) continue;
            if (selected == null || area.Priority > selected.Priority) selected = area;
        }
        return selected;
    }

    private void OnEnable()
    {
        if (!areas.Contains(this)) areas.Add(this);
    }

    private void OnDisable() => areas.Remove(this);
    private void OnDestroy() => areas.Remove(this);

    private void OnValidate()
    {
        size.x = Mathf.Max(.01f, size.x);
        size.y = Mathf.Max(.01f, size.y);
    }

    private void OnDrawGizmos()
    {
        Bounds bounds = WorldBounds;
        Color previous = Gizmos.color;
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
        Gizmos.color = previous;
    }
}
