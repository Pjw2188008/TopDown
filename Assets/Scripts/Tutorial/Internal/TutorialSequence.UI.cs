using UnityEngine;
using UnityEngine.UI;

/// <summary>씬에서 직접 편집하는 Canvas UI 연결입니다. 문구/표시만 갱신하며 패널 배치를 덮어쓰지 않습니다.</summary>
public sealed partial class TutorialSequence
{
    [Header("Canvas 안내 UI")]
    [SerializeField, Tooltip("안내 표시 여부입니다. 플레이어 조작과 시간은 바꾸지 않습니다.")] private bool showInstructions = true;
    [SerializeField, Tooltip("직접 편집할 안내 패널입니다. 이 스크립트 자신이나 부모 오브젝트는 연결하지 마세요.")] private GameObject instructionPanel;
    [SerializeField, Tooltip("단계 문구를 표시할 UI Text입니다. 위치/폰트/색상은 Text와 RectTransform에서 직접 편집하세요.")] private Text messageText;
    [SerializeField, Tooltip("선택적 단계 번호 UI Text입니다.")] private Text titleText;
    [Header("목표 표식 UI")]
    [SerializeField] private bool showDestinationLabel = true;
    [SerializeField, Tooltip("선택적 목표 표식입니다. Text나 Image를 원하는 모양으로 편집할 수 있습니다.")] private RectTransform destinationMarker;
    [SerializeField, Tooltip("켜면 표식의 위치만 현재 목표를 따라갑니다. 끄면 RectTransform에서 정한 위치를 유지합니다. 안내 패널 위치에는 영향이 없습니다.")] private bool followDestination = true;
    [SerializeField, Tooltip("목표 구역 상단에서 표식을 띄울 화면 거리입니다(Canvas 기준 픽셀).")] private Vector2 destinationOffset = new Vector2(0, 20);
    [SerializeField, Tooltip("목표를 비추는 카메라입니다. 비우면 MainCamera를 사용합니다.")] private Camera targetCamera;

    // 이전 IMGUI 설정은 편집기에서 Canvas를 처음 만들 때만 이전합니다.
    [SerializeField, HideInInspector] private bool pinPanelToTopRight = true;
    [SerializeField, HideInInspector] private Vector2 panelPosition = new Vector2(20, 190);
    [SerializeField, HideInInspector] private Vector2 panelSize = new Vector2(400, 150);
    [SerializeField, HideInInspector] private Font instructionFont;
    [SerializeField, HideInInspector] private int fontSize = 20;
    private Text overriddenMarkerText;
    private string originalMarkerText;

    private void Update() { RefreshTutorialUI(); }

    private void RefreshTutorialUI()
    {
        bool visible = isActiveAndEnabled && running && showInstructions && CurrentStep != null && Time.timeScale > 0f;
        SetUIVisible(instructionPanel, visible);
        if (visible)
        {
            if (messageText != null && messageText.text != CurrentMessage) messageText.text = CurrentMessage;
            string title = $"튜토리얼 {currentStepIndex + 1} / {steps.Length}";
            if (titleText != null && titleText.text != title) titleText.text = title;
        }
        bool hasTarget = TryGetTutorialTargetPoint(out Vector3 targetPoint);
        bool markerVisible = visible && showDestinationLabel && destinationMarker != null && hasTarget;
        SetCutMarkerText(markerVisible && CurrentStep.completionCondition == CompletionCondition.CutSucceeded);
        if (markerVisible && followDestination) markerVisible = PositionDestinationMarker();
        if (destinationMarker != null) SetUIVisible(destinationMarker.gameObject, markerVisible);
    }

    private bool PositionDestinationMarker()
    {
        var camera = targetCamera != null ? targetCamera : Camera.main;
        var parent = destinationMarker.parent as RectTransform;
        var canvas = destinationMarker.GetComponentInParent<Canvas>();
        if (camera == null || parent == null || canvas == null) return false;
        if (!TryGetTutorialTargetPoint(out Vector3 target)) return false;
        Vector3 point = camera.WorldToScreenPoint(target);
        if (point.z <= 0 || point.x < 0 || point.x > Screen.width || point.y < 0 || point.y > Screen.height) return false;
        Canvas root = canvas.rootCanvas;
        Camera uiCamera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
        Vector2 screenPoint = (Vector2)point + destinationOffset * root.scaleFactor;
        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, screenPoint, uiCamera, out Vector3 world)) return false;
        destinationMarker.position = world;
        return true;
    }

    private bool TryGetTutorialTargetPoint(out Vector3 point)
    {
        point = Vector3.zero;
        var step = CurrentStep;
        if (step == null) return false;
        if (step.completionCondition == CompletionCondition.ArrivalArea)
        {
            var area = step.arrivalArea;
            if (area == null || !area.enabled || !area.gameObject.activeInHierarchy) return false;
            point = new Vector3(area.bounds.center.x, area.bounds.max.y, area.transform.position.z);
            return true;
        }
        if (step.completionCondition != CompletionCondition.CutSucceeded || step.cutTarget == null || !step.cutTarget.activeInHierarchy) return false;
        if (step.cutTargetMarkerAnchor != null) { point = step.cutTargetMarkerAnchor.position; return true; }
        var renderer = step.cutTarget.GetComponentInChildren<Renderer>();
        if (renderer != null && renderer.enabled)
        { point = new Vector3(renderer.bounds.center.x, renderer.bounds.max.y, renderer.bounds.center.z); return true; }
        var collider = step.cutTarget.GetComponentInChildren<Collider2D>();
        if (collider != null && collider.enabled)
        { point = new Vector3(collider.bounds.center.x, collider.bounds.max.y, step.cutTarget.transform.position.z); return true; }
        point = step.cutTarget.transform.position;
        return true;
    }

    private void SetCutMarkerText(bool showCut)
    {
        Text label = showCut && destinationMarker != null ? destinationMarker.GetComponent<Text>() : null;
        if (overriddenMarkerText != label)
        {
            if (overriddenMarkerText != null) overriddenMarkerText.text = originalMarkerText;
            overriddenMarkerText = label;
            if (label != null) originalMarkerText = label.text;
        }
        if (label != null) label.text = "Cut 대상 ↓";
    }

    private void SetUIVisible(GameObject target, bool visible)
    {
        // 誤接続でチュートリアル自身を無効化しないようにします。
        if (target == null || transform.IsChildOf(target.transform)) return;
        if (target.activeSelf != visible) target.SetActive(visible);
    }

    private void HideTutorialUI()
    {
        SetCutMarkerText(false);
        SetUIVisible(instructionPanel, false);
        if (destinationMarker != null) SetUIVisible(destinationMarker.gameObject, false);
    }
}
