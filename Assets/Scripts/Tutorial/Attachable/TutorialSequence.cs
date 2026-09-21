using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// 시작 안내 → 지정 구역 도착 → 다음 안내를 관리합니다. 빈 Tutorial 오브젝트에 부착합니다.
/// 플레이어 입력/속도/시간을 변경하지 않고, 지정한 도착 구역도 자동 생성/수정하지 않습니다.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed partial class TutorialSequence : MonoBehaviour
{
    // 0번 값을 유지하여 기존 이동 튜토리얼의 직렬화 데이터와 호환합니다.
    public enum CompletionCondition { ArrivalArea = 0, EditModeEnabled = 1, CutSucceeded = 2, Manual = 3, Confirm = 4 }
    [Serializable]
    public sealed class Step
    {
        [Tooltip("완료 조건: ArrivalArea=구역 도착, EditModeEnabled=편집 모드 활성, CutSucceeded=실제 Cut 저장 성공, Manual=외부 호출, Confirm=Enter 확인.")]
        public CompletionCondition completionCondition;
        [Tooltip("안내를 보여 줄 최소 실제 시간(초)입니다. 편집 모드 슬로모션에는 영향받지 않으며 게임 일시 정지 중에는 흐르지 않습니다.")]
        [Min(0f)] public float minimumDisplaySeconds;
        [Tooltip("이 단계에서 화면에 표시할 안내입니다.")]
        [TextArea(2, 6)] public string message;
        [Tooltip("플레이어 중심이 도착해야 할 구역입니다. 직접 만든 BoxCollider2D(Is Trigger)를 연결하세요. 비워 두면 자동 진행하지 않고 CompleteCurrentStep 호출을 기다립니다.")]
        public BoxCollider2D arrivalArea;
        [Tooltip("CutSucceeded 단계에서 Cut해야 할 원본 오브젝트입니다. 자식에 붙은 오류도 인정합니다. 비우면 기존처럼 어떤 원본이든 Cut 성공을 인정합니다.")]
        public GameObject cutTarget;
        [Tooltip("플레이어에게 보여 줄 대상 이름입니다. 예: 거대한 상자. 비우면 오브젝트 이름을 사용합니다. Message의 {target}에 들어갑니다.")]
        public string cutTargetDisplayName;
        [Tooltip("선택적 목표 표식 위치입니다. 비우면 대상 Renderer/Collider의 위쪽에 표시합니다.")]
        public Transform cutTargetMarkerAnchor;
        [Tooltip("이 단계가 시작될 때 한 번 호출됩니다. 다음 튜토리얼 오브젝트 활성화 등에 연결할 수 있습니다. 이 이벤트 안에서 단계 전환을 재호출하지 마세요.")]
        public UnityEvent onEntered = new UnityEvent();
    }

    [Header("진행")]
    [Tooltip("안내할 플레이어입니다. 비워 두면 활성 PlayerMove를 검색합니다. 일반 단일 플레이어용입니다.")]
    [SerializeField] private PlayerMove player;
    [Tooltip("Play 시작 시 0번 단계부터 시작합니다. 해제하면 BeginTutorial()로 시작합니다. 진행 상황은 저장하지 않습니다.")]
    [SerializeField] private bool beginOnStart = true;
    [Tooltip("순서대로 실행할 단계입니다. 마지막 단계가 완료되면 안내를 숨기고 On Completed를 호출합니다.")]
    [SerializeField] private Step[] steps = {
        new Step { message = "WASD를 이용하여 이동하세요.\nSpace를 꾹 누르면 달릴 수 있어요.\n표시된 목표 지점까지 이동하세요." },
        new Step { message = "이동 연습 완료!\n다음 튜토리얼 안내를 여기에 설정하세요." }
    };
    [Tooltip("모든 단계 완료 시 한 번 호출됩니다. 다음 튜토리얼 시작 함수 등을 연결하세요.")]
    [SerializeField] private UnityEvent onCompleted = new UnityEvent();

    private int currentStepIndex = -1;
    private bool running;
    private bool transitioning;
    private Vector2 previousPosition;
    private bool hasPreviousPosition;
    private PlayerMove trackedPlayer;
    private int teleportVersion;
    private float nextPlayerSearch;
    private readonly List<RaycastHit2D> arrivalHits = new List<RaycastHit2D>(4);
    private float stepDisplayTime;
    private PlayerMove cutObservedPlayer;
    private int cutVersionAtEntry;
    private string capturedCutNames = string.Empty;
    private int cutTargetInstanceId;
    private bool requiresSpecificCutTarget;
    private string capturedTargetName = string.Empty;

    public int CurrentStepIndex => currentStepIndex;
    public bool IsRunning => running;
    public string CurrentMessage
    {
        get
        {
            var step = CurrentStep; if (step == null) return string.Empty;
            string template = step.message ?? string.Empty;
            string targetName = GetCutTargetName(step);
            string result = template.Replace("{error}", string.IsNullOrEmpty(capturedCutNames) ? "Cut한 오류" : capturedCutNames).Replace("{target}", targetName);
            if (step.completionCondition == CompletionCondition.CutSucceeded && requiresSpecificCutTarget && !template.Contains("{target}"))
                result = "Cut 대상: " + targetName + "\n" + result;
            return result;
        }
    }

    private string GetCutTargetName(Step step)
    {
        if (!string.IsNullOrWhiteSpace(step.cutTargetDisplayName)) return step.cutTargetDisplayName;
        if (step.cutTarget != null) return step.cutTarget.name;
        return string.IsNullOrEmpty(capturedTargetName) ? "오류 원본" : capturedTargetName;
    }
    private Step CurrentStep => running && steps != null && currentStepIndex >= 0 && currentStepIndex < steps.Length ? steps[currentStepIndex] : null;

    private void Start() { if (beginOnStart) BeginTutorial(); }
    private void OnEnable() { hasPreviousPosition = false; }
    private void OnDisable() { hasPreviousPosition = false; if (Application.isPlaying) HideTutorialUI(); }

    /// <summary>첫 안내부터 다시 시작합니다. PlayerPrefs나 플레이어 상태는 변경하지 않습니다.</summary>
    public void BeginTutorial()
    {
        if (transitioning) return;
        running = steps != null && steps.Length > 0;
        currentStepIndex = running ? 0 : -1;
        hasPreviousPosition = false;
        capturedCutNames = string.Empty;
        capturedTargetName = string.Empty;
        if (running) EnterCurrentStep();
        else RefreshTutorialUI();
    }

    /// <summary>구역 외의 조건을 만들 때 UnityEvent에서 호출할 수 있는 다음 단계 함수입니다.</summary>
    public void CompleteCurrentStep()
    {
        if (!running || transitioning) return;
        bool preserveCutBaseline = CurrentStep != null && CurrentStep.completionCondition == CompletionCondition.EditModeEnabled;
        hasPreviousPosition = false;
        currentStepIndex++;
        if (steps == null || currentStepIndex >= steps.Length)
        {
            running = false;
            transitioning = true;
            try { onCompleted.Invoke(); } finally { transitioning = false; RefreshTutorialUI(); }
        }
        else EnterCurrentStep(preserveCutBaseline && CurrentStep != null && CurrentStep.completionCondition == CompletionCondition.CutSucceeded);
    }

    public void StopTutorial() { running = false; hasPreviousPosition = false; RefreshTutorialUI(); }

    private void EnterCurrentStep(bool preserveCutBaseline = false)
    {
        stepDisplayTime = 0f;
        if (player == null) player = FindFirstObjectByType<PlayerMove>();
        if (!preserveCutBaseline || cutObservedPlayer != player)
        {
            cutObservedPlayer = player;
            cutVersionAtEntry = player != null ? player.SuccessfulCutVersion : 0;
        }
        // 편집 모드 안내 중 먼저 Cut해도 목표를 기억합니다. 이후 대상이 파괴돼도 '아무 대상' 조건으로 바뀌지 않습니다.
        if (!preserveCutBaseline)
        {
            var targetStep = CurrentStep;
            if (targetStep != null && targetStep.completionCondition == CompletionCondition.EditModeEnabled
                && steps != null && currentStepIndex + 1 < steps.Length
                && steps[currentStepIndex + 1]?.completionCondition == CompletionCondition.CutSucceeded)
                targetStep = steps[currentStepIndex + 1];
            requiresSpecificCutTarget = targetStep != null && targetStep.cutTarget != null;
            cutTargetInstanceId = requiresSpecificCutTarget ? targetStep.cutTarget.GetInstanceID() : 0;
            capturedTargetName = targetStep != null ? GetCutTargetName(targetStep) : string.Empty;
        }
        transitioning = true;
        try { CurrentStep?.onEntered?.Invoke(); } finally { transitioning = false; RefreshTutorialUI(); }
    }

    private void LateUpdate()
    {
        if (!running) return;
        if (player == null && Time.unscaledTime >= nextPlayerSearch)
        {
            nextPlayerSearch = Time.unscaledTime + .5f;
            player = FindFirstObjectByType<PlayerMove>();
        }
        if (trackedPlayer != player) { trackedPlayer = player; hasPreviousPosition = false; }
        if (player == null || !player.isActiveAndEnabled || Time.timeScale <= 0f || player.IsInstantlyDead)
        { hasPreviousPosition = false; return; }
        if (cutObservedPlayer != player)
        {
            cutObservedPlayer = player;
            cutVersionAtEntry = player.SuccessfulCutVersion;
        }
        stepDisplayTime += Time.unscaledDeltaTime;
        var step = CurrentStep;
        if (step == null) return;
        if (step.completionCondition != CompletionCondition.ArrivalArea)
        {
            hasPreviousPosition = false;
            if (stepDisplayTime < Mathf.Max(0f, step.minimumDisplaySeconds)) return;
            bool complete = false;
            switch (step.completionCondition)
            {
                case CompletionCondition.EditModeEnabled: complete = player.IsInEditMode; break;
                case CompletionCondition.CutSucceeded:
                    if (requiresSpecificCutTarget)
                    {
                        complete = player.TryGetTargetCutAfter(cutTargetInstanceId, cutVersionAtEntry, out string targetErrors);
                        if (complete) capturedCutNames = targetErrors;
                    }
                    else
                    {
                        complete = player.SuccessfulCutVersion != cutVersionAtEntry;
                        if (complete) capturedCutNames = player.LastSuccessfulCutNames;
                    }
                    break;
                case CompletionCondition.Confirm:
                    complete = Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame);
                    break;
            }
            if (complete) CompleteCurrentStep();
            return;
        }
        Vector2 position = player.transform.position;
        if (teleportVersion != player.HazardTeleportVersion) hasPreviousPosition = false;
        var area = CurrentStep?.arrivalArea;
        bool reached = false;
        if (area != null && area.enabled && area.isTrigger && area.gameObject.activeInHierarchy
            && (area.attachedRigidbody == null || area.attachedRigidbody.simulated))
        {
            Physics2D.SyncTransforms();
            reached = area.OverlapPoint(position);
            // 대시로 얇은 목표를 지나쳐도 도착으로 인정합니다. 워프/리스폰 경로는 제외합니다.
            Vector2 movement = position - previousPosition;
            if (!reached && hasPreviousPosition && movement.sqrMagnitude > .000001f)
            {
                // 월드 선분이 지정한 구역과 교차했는지만 확인합니다.
                reached = SegmentReachesArea(area, previousPosition, position);
            }
        }
        previousPosition = position;
        teleportVersion = player.HazardTeleportVersion;
        hasPreviousPosition = true;
        if (reached && stepDisplayTime >= Mathf.Max(0f, step.minimumDisplaySeconds)) CompleteCurrentStep(); // 한 프레임에 최대 한 단계만 진행합니다.
    }

    private bool SegmentReachesArea(BoxCollider2D area, Vector2 start, Vector2 end)
    {
        arrivalHits.Clear();
        Vector2 delta = end - start;
        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(1 << area.gameObject.layer);
        Physics2D.Raycast(start, delta.normalized, filter, arrivalHits, delta.magnitude);
        foreach (var hit in arrivalHits) if (hit.collider == area) return true;
        return false;
    }

    private void OnDrawGizmos()
    {
        if (steps == null) return;
        var oldMatrix = Gizmos.matrix; var oldColor = Gizmos.color;
        for (int i = 0; i < steps.Length; i++)
        {
            var area = steps[i]?.arrivalArea; if (area == null || steps[i].completionCondition != CompletionCondition.ArrivalArea) continue;
            Gizmos.color = Application.isPlaying && i == currentStepIndex ? Color.green : Color.cyan;
            Gizmos.matrix = area.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(area.offset, new Vector3(area.size.x, area.size.y, 0f));
        }
        Gizmos.matrix = oldMatrix; Gizmos.color = oldColor;
    }
}
