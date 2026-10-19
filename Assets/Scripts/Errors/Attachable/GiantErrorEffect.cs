using UnityEngine;

/// <summary>
/// 거대화 오류의 활성 상태와 대상 크기 변화를 관리합니다.
/// 처음부터 거대화된 오류 원본에 직접 부착하거나, 환경 Paste 시 PlayerMove가 대상에 자동으로 추가합니다.
/// </summary>
public class GiantErrorEffect : MonoBehaviour, IErrorSource
{
    public enum GrowthAnchorMode { AutoSideView, Pivot, Bottom }

    [Header("거대화 오류")]
    [Tooltip("거대화가 적용된 대상의 최종 크기 배율입니다. Cut하면 이 배율이 오류 정보로 보관됩니다.")]
    [SerializeField] private float targetMultiplier = 3f;

    [Tooltip("원래 크기에서 목표 크기까지 커지는 속도입니다. 값이 높을수록 거대화 연출이 빨리 끝납니다.")]
    [SerializeField] private float growthSpeed = 1.5f;

    [Tooltip("씬 시작과 동시에 거대화 오류를 적용할지 결정합니다. 오류를 받을 Paste 대상이라면 끄는 것을 권장합니다.")]
    [SerializeField] private bool triggerOnStart = true;

    [Tooltip("거대화 진행도에 따른 크기 변화 곡선입니다. 가로축은 시간, 세로축은 적용 비율입니다.")]
    [SerializeField] private AnimationCurve expansionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("AutoSideView는 2D 사이드뷰에서만 밑면을 유지합니다. Pivot은 기존 중심 확대, Bottom은 시점과 무관하게 밑면 기준 확대입니다.")]
    [SerializeField] private GrowthAnchorMode growthAnchor = GrowthAnchorMode.AutoSideView;
    [Tooltip("밑면 기준으로 쓸 몸체 Collider2D입니다(선택). 비우면 자신/자식의 활성 비트리거 Collider 최하단을 사용합니다. 감지용 Trigger는 제외하고, Collider가 없으면 SpriteRenderer 밑면을 사용합니다.")]
    [SerializeField] private Collider2D bottomAnchorCollider;

    private Vector3 baseScale;
    private Vector3 growthStartScale;
    private bool preserveBottom;
    private bool isGrowing;
    private float growthProgress;
    private bool awakeCompleted;

    public float CurrentMultiplier => targetMultiplier;
    private ErrorEffectState state;
    public bool IsActive => state.IsActive;
    public bool IsPasted => state.IsPasted;
    public bool CanCut => state.CanCut;
    public StoredErrorType ErrorType => StoredErrorType.Giant;
    public float StoredMultiplier => CurrentMultiplier;
    public void RemoveError() => ResetScale();

    public static bool CanPasteTo(PasteTargetType targetType) => ErrorRules.CanPasteTo(StoredErrorType.Giant, targetType);

    private void Awake()
    {
        baseScale = transform.localScale;
        growthStartScale = baseScale;
        awakeCompleted = true;

        if (triggerOnStart)
        {
            Trigger(targetMultiplier);
        }
    }

    public void Trigger(float multiplier)
    {
        if (multiplier <= 0f)
        {
            multiplier = 1f;
        }

        targetMultiplier = multiplier;
        growthStartScale = transform.localScale;
        preserveBottom = ResolveBottomAnchor(IsSideViewScene());
        state.ActivateOriginal();
        isGrowing = true;
        growthProgress = 0f;
    }

    /// <summary>붙여넣기로 적용한 오류는 다시 Cut할 수 없습니다.</summary>
    public void ApplyPaste(float multiplier)
    {
        Trigger(multiplier);
        state.MarkPasted();
    }

    /// <summary>Pasteしたプレイヤーの視点を使い、複数プレイヤーがいる場合も自動設定を確定します。</summary>
    public void ApplyPaste(float multiplier, bool sideView)
    {
        ApplyPaste(multiplier);
        preserveBottom = ResolveBottomAnchor(sideView);
    }

    private bool ResolveBottomAnchor(bool sideView)
        => growthAnchor == GrowthAnchorMode.Bottom || (growthAnchor == GrowthAnchorMode.AutoSideView && sideView);

    private bool IsSideViewScene()
    {
        var player = FindFirstObjectByType<PlayerMove>();
        return player != null && player.IsSideView && !player.IsSideViewDepthMovement;
    }

    public void ResetScale()
    {
        ApplyScale(baseScale);
        state.Deactivate();
        isGrowing = false;
        growthProgress = 0f;
    }

    /// <summary>크기 변화 도중에도 원본/Paste 출처와 진행도를 복구합니다. 직접 호출할 필요 없습니다.</summary>
    internal System.Action CaptureTutorialState()
    {
        Vector3 savedBase = awakeCompleted ? baseScale : transform.localScale;
        var savedState = state;
        bool savedGrowing = isGrowing;
        float savedProgress = growthProgress;
        float savedMultiplier = targetMultiplier;
        Vector3 savedStartScale = awakeCompleted ? growthStartScale : transform.localScale;
        bool savedPreserveBottom = preserveBottom;
        if (!awakeCompleted && triggerOnStart)
        {
            savedState.ActivateOriginal();
            savedGrowing = true;
            savedPreserveBottom = ResolveBottomAnchor(IsSideViewScene());
        }
        return () =>
        {
            if (this == null) return;
            baseScale = savedBase;
            state = savedState;
            isGrowing = savedGrowing;
            growthProgress = savedProgress;
            targetMultiplier = savedMultiplier;
            growthStartScale = savedStartScale;
            preserveBottom = savedPreserveBottom;
        };
    }

    private void Update()
    {
        if (!isGrowing)
        {
            return;
        }

        AdvanceGrowth(Time.deltaTime);
    }

    private void AdvanceGrowth(float deltaTime)
    {
        if (!isGrowing || deltaTime <= 0f) return;
        growthProgress += deltaTime * growthSpeed;
        float t = Mathf.Clamp01(growthProgress);
        float eased = expansionCurve.Evaluate(t);

        Vector3 targetScale = baseScale * targetMultiplier;
        ApplyScale(Vector3.Lerp(growthStartScale, targetScale, eased));

        if (t >= 1f)
        {
            isGrowing = false;
        }
    }

    private void ApplyScale(Vector3 scale)
    {
        if (transform.localScale == scale) return;
        float bottomBefore = 0f;
        bool anchored = preserveBottom && TryGetWorldBottom(out bottomBefore);
        transform.localScale = scale;
        if (anchored && TryGetWorldBottom(out float bottomAfter))
        {
            Vector3 position = transform.position;
            position.y += bottomBefore - bottomAfter;
            if (TryGetComponent<Rigidbody2D>(out var body)) body.position = position;
            transform.position = position;
        }
        // Auto Sync Transformsが無効でも次の移動Castから新しい大きさ/位置を使えます。
        Physics2D.SyncTransforms();
    }

    private bool TryGetWorldBottom(out float bottom)
    {
        Physics2D.SyncTransforms();
        bottom = float.PositiveInfinity;
        if (IsUsableBodyCollider(bottomAnchorCollider))
        {
            bottom = bottomAnchorCollider.bounds.min.y;
            return true;
        }
        var shapes = UnityEngine.Pool.ListPool<Collider2D>.Get();
        try
        {
            GetComponentsInChildren(false, shapes);
            foreach (var shape in shapes)
                if (IsUsableBodyCollider(shape)) bottom = Mathf.Min(bottom, shape.bounds.min.y);
        }
        finally { UnityEngine.Pool.ListPool<Collider2D>.Release(shapes); }
        if (!float.IsPositiveInfinity(bottom)) return true;

        var renderers = UnityEngine.Pool.ListPool<SpriteRenderer>.Get();
        try
        {
            GetComponentsInChildren(false, renderers);
            foreach (var renderer in renderers)
                if (renderer.enabled && renderer.sprite != null && renderer.gameObject.activeInHierarchy)
                    bottom = Mathf.Min(bottom, renderer.bounds.min.y);
        }
        finally { UnityEngine.Pool.ListPool<SpriteRenderer>.Release(renderers); }
        return !float.IsPositiveInfinity(bottom);
    }

    private bool IsUsableBodyCollider(Collider2D shape)
        => shape != null && shape.enabled && !shape.isTrigger && shape.gameObject.activeInHierarchy
            && (shape.transform == transform || shape.transform.IsChildOf(transform))
            && (shape.attachedRigidbody == null || shape.attachedRigidbody.simulated)
            && shape.bounds.size.y > .00001f;
}
