using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 주변 밝기는 보존하고 멀리 있는 게임 화면을 부드럽게 어둡게 합니다.
/// CameraFollow의 일부이며 따로 부착하지 않습니다. 광원/피격/은신 머티리얼은 변경하지 않습니다.
/// </summary>
public partial class CameraFollow
{
    [Header("플레이어 주변 빛")]
    [Tooltip("플레이어 주변은 밝게 유지하고 먼 부분은 어둡게 합니다. Game 화면에서 실행 중 적용됩니다. UI와 구역 전환 페이드는 그대로 유지합니다.")]
    [SerializeField] private bool usePlayerLight = true;
    [Tooltip("먼 배경의 어두운 정도입니다. 0은 원래 밝기, 1은 Darkness Color로 완전히 가립니다.")]
    [Range(0f, 1f), SerializeField] private float backgroundDarkness = .65f;
    [Tooltip("어두운 부분의 색입니다. 알파 대신 Background Darkness로 강도를 조절합니다.")]
    [SerializeField] private Color darknessColor = new Color(.015f, .02f, .04f, 1f);
    [Tooltip("플레이어 주변이 원래 밝기로 보이는 반경(월드 단위)입니다.")]
    [Min(0f), SerializeField] private float lightInnerRadius = 1.1f;
    [Tooltip("완전히 어두워지는 바깥 반경(월드 단위)입니다. 안쪽과 바깥 반경 사이를 부드럽게 이어줍니다.")]
    [Min(.01f), SerializeField] private float lightOuterRadius = 4.5f;
    [Tooltip("플레이어 Transform 기준 빛의 중심 위치입니다. 발 피벗이면 Y를 올려 몸 중앙에 맞추세요. 좌우 플립의 영향을 받지 않습니다.")]
    [SerializeField] private Vector2 playerLightOffset = new Vector2(0f, .5f);
    [Tooltip("카메라 선택 시 Scene 뷰에서 밝은 반경과 바깥 반경을 표시합니다.")]
    [SerializeField] private bool showPlayerLightGizmos = true;

    private Canvas playerLightCanvas;
    private Material playerLightMaterial;
    private bool warnedPlayerLightShader;

    private void UpdatePlayerLight()
    {
        // 현재 프로젝트의 XY 직교 카메라용. 대상 소실/비활성/카메라 변경 시 검은 화면을 남기지 않습니다.
        if (!usePlayerLight || backgroundDarkness <= 0f || player == null || !player.gameObject.activeInHierarchy
            || viewCamera == null || !viewCamera.isActiveAndEnabled || !viewCamera.orthographic
            || Mathf.Abs(Vector3.Dot(transform.forward, Vector3.forward)) < .9999f)
        {
            HidePlayerLight();
            return;
        }
        Vector3 center = viewCamera.WorldToViewportPoint(player.position + (Vector3)playerLightOffset);
        if (center.z <= 0f) { HidePlayerLight(); return; }
        if (playerLightCanvas == null && !CreatePlayerLight()) return;

        float inner = Mathf.Max(0f, lightInnerRadius);
        float outer = Mathf.Max(inner + .01f, lightOuterRadius);
        Rect viewport = viewCamera.rect;
        playerLightMaterial.SetVector("_Center", new Vector4(center.x, center.y, 0f, 0f));
        playerLightMaterial.SetVector("_ViewSize", new Vector4(2f * viewCamera.orthographicSize * viewCamera.aspect,
            2f * viewCamera.orthographicSize, 0f, 0f));
        playerLightMaterial.SetVector("_Viewport", new Vector4(viewport.x, viewport.y, viewport.width, viewport.height));
        playerLightMaterial.SetVector("_Radii", new Vector4(inner, outer, 0f, 0f));
        playerLightMaterial.SetColor("_DarknessColor", darknessColor);
        playerLightMaterial.SetFloat("_Darkness", Mathf.Clamp01(backgroundDarkness));
        playerLightCanvas.targetDisplay = viewCamera.targetDisplay;
        playerLightCanvas.enabled = true;
    }

    private bool CreatePlayerLight()
    {
        var shader = Resources.Load<Shader>("Rendering/PlayerFocusLight");
        if (shader == null || !shader.isSupported)
        {
            if (!warnedPlayerLightShader)
                Debug.LogWarning("PlayerFocusLight 셰이더를 찾을 수 없거나 지원하지 않아 주변 빛을 표시하지 않습니다.", this);
            warnedPlayerLightShader = true;
            return false;
        }
        playerLightMaterial = new Material(shader) { name = "Player Focus Light (Runtime)", hideFlags = HideFlags.DontSave };
        var root = new GameObject("Player Focus Light (Runtime)", typeof(RectTransform), typeof(Canvas));
        root.hideFlags = HideFlags.DontSave;
        root.transform.SetParent(transform, false);
        playerLightCanvas = root.GetComponent<Canvas>();
        playerLightCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // 일반 HUD(Canvas 기본 0)보다 먼저 표시. 카메라 암전(32760)은 이 위에 표시됩니다.
        playerLightCanvas.sortingOrder = -32700;
        var panel = new GameObject("Soft Darkness", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        panel.transform.SetParent(root.transform, false);
        var image = panel.GetComponent<RawImage>();
        image.texture = Texture2D.whiteTexture;
        image.material = playerLightMaterial;
        image.raycastTarget = false;
        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return true;
    }

    private void HidePlayerLight()
    {
        if (playerLightCanvas != null) playerLightCanvas.enabled = false;
    }

    private void DestroyPlayerLight()
    {
        if (playerLightCanvas != null)
        {
            if (Application.isPlaying) Destroy(playerLightCanvas.gameObject);
            else DestroyImmediate(playerLightCanvas.gameObject);
        }
        if (playerLightMaterial != null)
        {
            if (Application.isPlaying) Destroy(playerLightMaterial);
            else DestroyImmediate(playerLightMaterial);
        }
        playerLightCanvas = null;
        playerLightMaterial = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (!usePlayerLight || !showPlayerLightGizmos || player == null) return;
        Vector3 center = player.position + (Vector3)playerLightOffset;
        Color previous = Gizmos.color;
        DrawPlayerLightCircle(center, Mathf.Max(0f, lightInnerRadius), new Color(1f, .9f, .4f, .9f));
        DrawPlayerLightCircle(center, Mathf.Max(lightInnerRadius + .01f, lightOuterRadius), new Color(.4f, .7f, 1f, .7f));
        Gizmos.color = previous;
    }

    private static void DrawPlayerLightCircle(Vector3 center, float radius, Color color)
    {
        Gizmos.color = color;
        Vector3 previous = center + Vector3.right * radius;
        for (int i = 1; i <= 64; i++)
        {
            float angle = i * Mathf.PI * 2f / 64f;
            Vector3 next = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
