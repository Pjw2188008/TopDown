using UnityEngine;

/// <summary>
/// Inspector에서 직접 편집한 SpriteRenderer의 표시와 지정 콜라이더의 기즈모를 관리합니다.
/// 이미지/색상/머티리얼/크기/정렬을 덮어쓰지 않습니다. 별도로 부착하지 않는 partial 구현입니다.
/// </summary>
public sealed partial class LaserHazard
{
    [Header("직접 편집할 빔")]
    [Tooltip("빔 SpriteRenderer입니다. 비우면 같은 오브젝트에서 찾습니다. 이 오브젝트 또는 자식만 연결하세요. Sprite/Color/크기/정렬은 SpriteRenderer와 Transform에서 직접 편집합니다.")]
    [SerializeField] private SpriteRenderer beamRenderer;

    // 이전 씬의 값을 보존합니다. 편집기 전환 버튼에서만 사용하며 실행 중에는 사용하지 않습니다.
    [SerializeField, HideInInspector] private Transform endPoint;
    [SerializeField, HideInInspector] private float length = 8f;
    [SerializeField, HideInInspector] private float width = .15f;
    [SerializeField, HideInInspector] private Sprite beamSprite;
    [SerializeField, HideInInspector] private bool spriteIsVertical;
    [SerializeField, HideInInspector] private Color spriteTint = Color.white;

    private SpriteRenderer controlledRenderer;
    private bool originalForceRenderingOff;

    private SpriteRenderer ResolveBeamRenderer()
    {
        if (beamRenderer == null) beamRenderer = GetComponent<SpriteRenderer>();
        return beamRenderer != null && beamRenderer.transform.IsChildOf(transform) ? beamRenderer : null;
    }

    private bool CanUseBeamCollider()
    {
        // 연결한 컴포넌트만 사용합니다. 자동 검색이나 보정은 하지 않습니다.
        return beamCollider != null && beamCollider.enabled && beamCollider.isTrigger
            && beamCollider.gameObject.activeInHierarchy && beamCollider.transform.IsChildOf(transform)
            && (beamCollider.attachedRigidbody == null || beamCollider.attachedRigidbody.simulated)
            && beamCollider.size.x > 0f && beamCollider.size.y > 0f;
    }

    private void UpdateBeamVisibility()
    {
        var renderer = ResolveBeamRenderer();
        if (controlledRenderer != renderer)
        {
            ReleaseBeamVisibility();
            controlledRenderer = renderer;
            if (renderer != null) originalForceRenderingOff = renderer.forceRenderingOff;
        }
        if (controlledRenderer != null)
            controlledRenderer.forceRenderingOff = originalForceRenderingOff || !IsLethal;
    }

    private void HideBeam()
    {
        UpdateBeamVisibility(); // IsLethal=false일 때 Color/Enabled 대신 렌더링만 숨깁니다.
    }

    private void ReleaseBeamVisibility()
    {
        if (controlledRenderer != null) controlledRenderer.forceRenderingOff = originalForceRenderingOff;
        controlledRenderer = null;
    }

    private void DrawBeamOutline(Vector3 offset)
    {
        if (beamCollider == null || !beamCollider.transform.IsChildOf(transform)) return;
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.Translate(offset) * beamCollider.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(beamCollider.offset, new Vector3(beamCollider.size.x, beamCollider.size.y, 0f));
        Gizmos.matrix = old;
    }
}
