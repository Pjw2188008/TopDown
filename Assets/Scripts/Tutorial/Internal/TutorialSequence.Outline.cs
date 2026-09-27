using UnityEngine;
using UnityEngine.Rendering;

/// <summary>현재 도착 구역의 게임 화면용 테두리입니다. 별도 부착하지 않는 TutorialSequence의 일부이며 Collider는 변경하지 않습니다.</summary>
public sealed partial class TutorialSequence
{
    [Header("목표 구역 테두리")]
    [SerializeField, Tooltip("현재 ArrivalArea 단계의 목표 구역을 게임 화면에 테두리로 표시합니다. Canvas 안내나 Gizmos를 꺼도 사용할 수 있습니다.")]
    private bool showDestinationOutline = true;
    [SerializeField, Tooltip("목표 구역 테두리 색상과 투명도입니다.")]
    private Color destinationOutlineColor = new Color(.2f, 1f, .65f, .9f);
    [SerializeField, Min(.001f), Tooltip("테두리 두께(월드 단위). 카메라가 확대되면 화면에서도 두꺼워집니다.")]
    private float destinationOutlineWidth = .05f;
    [SerializeField, Tooltip("배경보다 위에 표시할 Sorting Layer 이름입니다. 맵과 같은 레이어 또는 더 위의 레이어를 지정하세요.")]
    private string destinationOutlineSortingLayer = "Default";
    [SerializeField, Tooltip("같은 Sorting Layer 안에서의 표시 순서입니다. 배경에 가려지면 높이세요.")]
    private int destinationOutlineSortingOrder = 100;

    private LineRenderer destinationOutline;
    private Material destinationOutlineMaterial;
    private readonly Vector3[] destinationOutlineCorners = new Vector3[4];
    private bool warnedMissingOutlineShader;

    private void RefreshDestinationOutline()
    {
        var step = CurrentStep;
        var area = step?.arrivalArea;
        bool visible = Application.isPlaying && isActiveAndEnabled && running && showDestinationOutline
            && Time.timeScale > 0f && step != null && step.completionCondition == CompletionCondition.ArrivalArea
            && area != null && area.enabled && area.isTrigger && area.gameObject.activeInHierarchy
            && area.size.x > 0f && area.size.y > 0f
            && (area.attachedRigidbody == null || area.attachedRigidbody.simulated);
        if (!visible) { HideDestinationOutline(); return; }
        if (!EnsureDestinationOutline()) return;

        // 축에 정렬된 bounds 대신 Collider의 로컬 네 모서리를 변환합니다.
        // Offset, Z 회전, 부모의 크기/반전을 포함해 직접 지정한 범위를 따라갑니다.
        Vector2 half = area.size * .5f;
        Vector2 center = area.offset;
        destinationOutlineCorners[0] = area.transform.TransformPoint(center + new Vector2(-half.x, -half.y));
        destinationOutlineCorners[1] = area.transform.TransformPoint(center + new Vector2(-half.x, half.y));
        destinationOutlineCorners[2] = area.transform.TransformPoint(center + new Vector2(half.x, half.y));
        destinationOutlineCorners[3] = area.transform.TransformPoint(center + new Vector2(half.x, -half.y));
        destinationOutline.SetPositions(destinationOutlineCorners);
        destinationOutline.widthMultiplier = Mathf.Max(.001f, destinationOutlineWidth);
        destinationOutline.startColor = destinationOutline.endColor = destinationOutlineColor;
        destinationOutline.sortingLayerID = SortingLayer.NameToID(destinationOutlineSortingLayer);
        destinationOutline.sortingOrder = destinationOutlineSortingOrder;
        destinationOutline.enabled = true;
    }

    private bool EnsureDestinationOutline()
    {
        if (destinationOutline != null) return true;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            if (!warnedMissingOutlineShader)
                Debug.LogWarning("목표 구역 테두리에 필요한 Sprites/Default 셰이더를 찾지 못했습니다. 튜토리얼 진행은 유지됩니다.", this);
            warnedMissingOutlineShader = true;
            return false;
        }
        // 월드 좌표를 사용하고 부모 스케일에 의해 두께가 변하지 않도록 독립된 임시 오브젝트로 만듭니다.
        var borderObject = new GameObject("Tutorial Destination Outline") { hideFlags = HideFlags.DontSave };
        borderObject.layer = 2; // Ignore Raycast. 시각 요소이며 Collider/피해 판정을 생성하지 않습니다.
        destinationOutline = borderObject.AddComponent<LineRenderer>();
        destinationOutlineMaterial = new Material(shader) { name = "Tutorial Outline Material", hideFlags = HideFlags.HideAndDontSave };
        destinationOutline.sharedMaterial = destinationOutlineMaterial;
        destinationOutline.useWorldSpace = true;
        destinationOutline.loop = true;
        destinationOutline.positionCount = 4;
        destinationOutline.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
        destinationOutline.numCornerVertices = 2;
        destinationOutline.shadowCastingMode = ShadowCastingMode.Off;
        destinationOutline.receiveShadows = false;
        destinationOutline.textureMode = LineTextureMode.Stretch;
        return true;
    }

    private void HideDestinationOutline()
    {
        if (destinationOutline != null) destinationOutline.enabled = false;
    }

    private void OnDestroy()
    {
        // 이 인스턴스가 만든 표시물만 정리하고 사용자 Collider/Material은 유지합니다.
        HideDestinationOutline();
        if (destinationOutline != null) ReleaseOutlineObject(destinationOutline.gameObject);
        if (destinationOutlineMaterial != null) ReleaseOutlineObject(destinationOutlineMaterial);
    }

    private static void ReleaseOutlineObject(Object ownedObject)
    {
        if (Application.isPlaying) Destroy(ownedObject);
        else DestroyImmediate(ownedObject);
    }
}
