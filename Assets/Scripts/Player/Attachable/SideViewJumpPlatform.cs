using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2.5D 전용 한방향 발판입니다. 지정한 Trigger는 높이 0의 바닥 투영 범위이며,
/// 그 안에서 하강 중 높이를 통과한 플레이어를 받칩니다. 일반 벽/천장 판정은 아닙니다.
/// 발판 GameObject에 부착하고 Footprint에 BoxCollider2D를 직접 연결합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class SideViewJumpPlatform : MonoBehaviour
{
    [SerializeField, Tooltip("높이 0 기준 착지 범위입니다. BoxCollider2D의 Is Trigger를 켜세요. 자동 검색/추가하지 않습니다.")]
    private BoxCollider2D footprint;
    [SerializeField, Min(.01f), Tooltip("기본 바닥에서 발판 윗면까지의 높이(월드 단위)입니다. 플레이어 점프 높이보다 낮게 시작하세요.")]
    private float surfaceHeight = 1f;
    [SerializeField, Tooltip("선택적 윗면 그림입니다. Footprint와 같은 오브젝트의 자식으로 두세요. 높이/범위에 맞춰 배치됩니다.")]
    private SpriteRenderer topVisual;
    [SerializeField, Tooltip("선택적 바닥 범위 안내 그림입니다. Footprint와 같은 오브젝트의 자식으로 두세요.")]
    private SpriteRenderer footprintVisual;
    private static readonly HashSet<SideViewJumpPlatform> Active = new HashSet<SideViewJumpPlatform>();

    public float SurfaceHeight => Mathf.Max(.01f, surfaceHeight);
    private bool IsUsable => isActiveAndEnabled && footprint != null && footprint.enabled
        && footprint.gameObject.activeInHierarchy && footprint.isTrigger;

    private void OnEnable() { Active.Add(this); RefreshVisuals(); }
    private void OnDisable() => Active.Remove(this);
    private void OnDestroy() => Active.Remove(this);
    private void OnValidate() { surfaceHeight = Mathf.Max(.01f, surfaceHeight); RefreshVisuals(); }
    private void LateUpdate() => RefreshVisuals();

    /// <summary>발 위치 아래에 있는 가장 높은 표면을 찾습니다. 높이보다 위의 발판으로 순간이동하지 않습니다.</summary>
    public static float FindSupportHeight(Vector2 groundFeet, float maximumHeight)
    {
        Physics2D.SyncTransforms();
        float result = 0f;
        foreach (var platform in Active)
        {
            if (platform == null || !platform.IsUsable) continue;
            float height = platform.SurfaceHeight;
            if (height <= maximumHeight + .0001f && height > result && platform.footprint.OverlapPoint(groundFeet)) result = height;
        }
        return result;
    }

    private void RefreshVisuals()
    {
        if (footprint == null) return;
        PlaceVisual(footprintVisual, 0f);
        PlaceVisual(topVisual, SurfaceHeight);
    }

    private void PlaceVisual(SpriteRenderer visual, float height)
    {
        if (visual == null || visual.sprite == null || visual.transform.parent != footprint.transform) return;
        Vector3 size = visual.sprite.bounds.size;
        visual.transform.localPosition = (Vector3)footprint.offset
            + footprint.transform.InverseTransformVector(Vector3.up * height);
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = new Vector3(footprint.size.x / Mathf.Max(.0001f, size.x),
            footprint.size.y / Mathf.Max(.0001f, size.y), 1f);
    }

    private void OnDrawGizmosSelected()
    {
        if (footprint == null) return;
        var old = Gizmos.matrix;
        Gizmos.matrix = footprint.transform.localToWorldMatrix;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(footprint.offset, footprint.size);
        Vector3 heightOffset = footprint.transform.InverseTransformVector(Vector3.up * SurfaceHeight);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube((Vector3)footprint.offset + heightOffset, footprint.size);
        Gizmos.DrawLine(footprint.offset, (Vector3)footprint.offset + heightOffset);
        Gizmos.matrix = old;
    }
}
