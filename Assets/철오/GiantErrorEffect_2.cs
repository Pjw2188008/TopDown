using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GiantErrorEffect와 별도로 사용하는 거대화 효과입니다.
/// 크기가 커질 때 같은 오브젝트의 Rigidbody2D 질량도 2D 면적에 비례해 증가시킵니다.
/// 기존 GiantErrorEffect가 붙여넣기로 적용되면 크기는 그 효과에 맡기고 질량을 맞춥니다.
/// </summary>
[DefaultExecutionOrder(-100)]
public class GiantErrorEffect_2 : MonoBehaviour, IErrorSource
{
    [Header("효과 대상")]
    [SerializeField, Tooltip("거대화할 대상입니다. 비워 두면 이 컴포넌트가 붙은 오브젝트만 대상으로 합니다.")]
    private Transform effectTarget;

    [Header("거대화 설정")]
    [SerializeField, Min(0.01f)] private float targetMultiplier = 3f;
    [SerializeField, Min(0f)] private float growthSpeed = 1.5f;
    [Tooltip("오류 원본은 켜고, 붙여넣기를 받을 물체는 끄세요. 꺼도 기존 거대화로 바뀐 크기에 따라 질량은 갱신됩니다.")]
    [SerializeField] private bool triggerOnStart = true;
    [SerializeField] private AnimationCurve expansionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("원래 크기로 복귀")]
    [Tooltip("원래 크기로 돌아오는 속도입니다. 1이면 약 1초, 0.5이면 약 2초가 걸립니다. 질량도 크기에 맞춰 서서히 줄어듭니다.")]
    [SerializeField, Min(0.01f)] private float returnSpeed = 1f;

    [Header("밑면 고정")]
    [SerializeField] private bool preserveBottom = true;
    [SerializeField] private Collider2D bottomAnchorCollider;

    [Header("거대화 노이즈")]
    [SerializeField, Tooltip("원래 크기보다 커진 동안 노이즈를 표시합니다. 기존 거대화 붙여넣기로 커져도 적용됩니다.")]
    private bool showGrowthNoise = true;
    [SerializeField, Range(.02f, 1f), Tooltip("노이즈 적용 중 보이는 정도입니다. 1이면 가장 선명하게 보입니다.")]
    private float noiseOpacity = 1f;
    [SerializeField, Range(0f, 1f)] private float noiseStrength = .35f;
    [SerializeField, Min(1f), Tooltip("화면 픽셀 기준 노이즈 입자 크기입니다.")]
    private float noisePixelSize = 3f;
    [SerializeField, Min(0f), Tooltip("초당 노이즈 모양 변경 횟수입니다.")]
    private float noiseRate = 12f;
    [SerializeField, Tooltip("노이즈를 표시할 그림입니다. 비우면 자신과 자식의 SpriteRenderer를 사용합니다.")]
    private SpriteRenderer[] noiseRenderers;

    private struct NoiseVisual
    {
        public SpriteRenderer renderer;
        public Material original;
    }
    private readonly List<NoiseVisual> noiseVisuals = new List<NoiseVisual>();
    private Material noiseMaterial;
    private bool warnedMissingNoiseShader;

    private Vector3 baseScale;
    private Vector3 growthStartScale;
    private Rigidbody2D body;
    private float baseMass;
    private bool isGrowing;
    private bool isReturning;
    private float growthProgress;
    private ErrorEffectState state;
    private GiantErrorEffect existingGiant;

    private Transform TargetTransform => effectTarget != null ? effectTarget : transform;

    public float CurrentMultiplier => targetMultiplier;
    // 기존 캐릭터의 Cut 처리에서 거대화 오류로 인식합니다.
    public StoredErrorType ErrorType => StoredErrorType.Giant;
    public float StoredMultiplier => CurrentMultiplier;
    public bool IsActive => state.IsActive && !ExistingGiantControlsScale();
    public bool IsPasted => state.IsPasted;
    public bool CanCut => state.CanCut && !ExistingGiantControlsScale();

    public void RemoveError() => ResetScale();

    private void Awake()
    {
        baseScale = TargetTransform.localScale;
        growthStartScale = baseScale;
        body = TargetTransform.GetComponent<Rigidbody2D>();
        if (body != null) baseMass = body.mass;

        if (triggerOnStart) Trigger(targetMultiplier);
    }

    /// <summary>지정한 배율로 키우고, 크기에 맞춰 Rigidbody2D 질량도 증가시킵니다.</summary>
    public void Trigger(float multiplier)
    {
        if (FollowExistingGiant()) return;
        targetMultiplier = Mathf.Max(0.01f, multiplier);
        growthStartScale = TargetTransform.localScale;
        state.ActivateOriginal();
        isReturning = false;
        isGrowing = true;
        growthProgress = 0f;
    }

    /// <summary>직접 붙여넣기로 적용한 효과는 다시 흡수할 수 없습니다.</summary>
    public void ApplyPaste(float multiplier)
    {
        if (FollowExistingGiant())
        {
            existingGiant.ApplyPaste(multiplier);
            return;
        }
        Trigger(multiplier);
        state.MarkPasted();
    }

    /// <summary>오류는 즉시 제거하고, 크기와 Rigidbody2D 질량은 서서히 처음 값으로 되돌립니다.</summary>
    public void ResetScale()
    {
        state.Deactivate();
        if (FollowExistingGiant())
        {
            existingGiant.ResetScale();
            UpdateMassFromScale();
            return;
        }
        if (isGrowing && isReturning) return;
        growthStartScale = TargetTransform.localScale;
        isReturning = true;
        isGrowing = true;
        growthProgress = 0f;
    }

    // 발판 이동과 충돌 계산 전에 크기/질량을 갱신합니다.
    private void FixedUpdate()
    {
        if (!FollowExistingGiant()) AdvanceOwnGrowth();
        // 발판이 질량을 읽기 전에 현재 크기에 맞춥니다. 증가를 누적하지 않습니다.
        UpdateMassFromScale();
    }

    private void LateUpdate()
    {
        // 기존 GiantErrorEffect는 Update에서 크기를 바꾸므로 그 뒤에도 질량을 갱신합니다.
        FollowExistingGiant();
        UpdateMassFromScale();
        UpdateNoiseVisuals();
    }

    private void UpdateNoiseVisuals()
    {
        Vector3 scale = TargetTransform.localScale;
        float sizeRatio = Mathf.Max(ScaleRatio(scale.x, baseScale.x), ScaleRatio(scale.y, baseScale.y));
        if (!showGrowthNoise || sizeRatio <= 1.001f)
        {
            RestoreNoiseVisuals();
            return;
        }

        if (noiseMaterial == null)
        {
            Shader shader = Resources.Load<Shader>("ErrorEffects/StealthNoise");
            if (shader == null)
            {
                if (!warnedMissingNoiseShader)
                    Debug.LogWarning("GiantErrorEffect_2: ErrorEffects/StealthNoise 셰이더를 찾을 수 없습니다.", this);
                warnedMissingNoiseShader = true;
                return;
            }
            noiseMaterial = new Material(shader) { name = "Giant Noise (Runtime)", hideFlags = HideFlags.DontSave };
        }
        // 원래 크기 근처에서는 노이즈를 부드럽게 줄입니다.
        float blend = Mathf.Clamp01((sizeRatio - 1f) / .25f);
        noiseMaterial.SetFloat("_StealthOpacity", Mathf.Lerp(1f, Mathf.Clamp(noiseOpacity, .02f, 1f), blend));
        noiseMaterial.SetFloat("_NoiseStrength", Mathf.Clamp01(noiseStrength) * blend);
        noiseMaterial.SetFloat("_NoisePixelSize", Mathf.Max(1f, noisePixelSize));
        noiseMaterial.SetFloat("_NoiseRate", Mathf.Max(0f, noiseRate));

        if (noiseVisuals.Count > 0) return;
        var renderers = noiseRenderers != null && noiseRenderers.Length > 0
            ? noiseRenderers : TargetTransform.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null
                || (renderer.transform != TargetTransform && !renderer.transform.IsChildOf(TargetTransform))
                || noiseVisuals.Exists(v => v.renderer == renderer)) continue;
            noiseVisuals.Add(new NoiseVisual { renderer = renderer, original = renderer.sharedMaterial });
            renderer.sharedMaterial = noiseMaterial;
        }
    }

    private void RestoreNoiseVisuals()
    {
        foreach (NoiseVisual visual in noiseVisuals)
            if (visual.renderer != null && visual.renderer.sharedMaterial == noiseMaterial)
                visual.renderer.sharedMaterial = visual.original;
        noiseVisuals.Clear();
    }

    private void OnDisable() => RestoreNoiseVisuals();

    private void OnDestroy()
    {
        RestoreNoiseVisuals();
        if (noiseMaterial != null) Destroy(noiseMaterial);
    }

    private bool ExistingGiantControlsScale()
    {
        // 붙여넣기 때 런타임에 추가되는 컴포넌트도 찾습니다.
        if (existingGiant == null) existingGiant = TargetTransform.GetComponent<GiantErrorEffect>();
        return existingGiant != null && existingGiant.IsActive;
    }

    private bool FollowExistingGiant()
    {
        if (!ExistingGiantControlsScale()) return false;
        isGrowing = false;
        isReturning = false;
        growthProgress = 0f;
        // 붙여넣기 상태와 Cut 가능 여부는 기존 효과가 관리합니다.
        // 이 컴포넌트가 중복 오류 원본으로 잡히지 않도록 비활성 처리합니다.
        state.Deactivate();
        return true;
    }

    private void UpdateMassFromScale()
    {
        if (body == null) return;
        Vector3 scale = TargetTransform.localScale;
        float xRatio = ScaleRatio(scale.x, baseScale.x);
        float yRatio = ScaleRatio(scale.y, baseScale.y);
        body.mass = Mathf.Max(0.0001f, baseMass * xRatio * yRatio);
    }

    private void AdvanceOwnGrowth()
    {
        if (!isGrowing) return;
        float speed = isReturning ? Mathf.Max(0.01f, returnSpeed) : growthSpeed;
        if (speed <= 0f) return;

        growthProgress += Time.fixedDeltaTime * speed;
        float t = Mathf.Clamp01(growthProgress);
        float eased = expansionCurve.Evaluate(t);
        Vector3 destination = isReturning ? baseScale : baseScale * targetMultiplier;
        ApplyScale(t >= 1f ? destination : Vector3.Lerp(growthStartScale, destination, eased));

        if (t >= 1f) isGrowing = false;
    }

    private void ApplyScale(Vector3 scale)
    {
        float bottomBefore = 0f;
        bool anchored = preserveBottom && TryGetWorldBottom(out bottomBefore);
        TargetTransform.localScale = scale;

        UpdateMassFromScale();

        if (anchored && TryGetWorldBottom(out float bottomAfter))
        {
            Vector3 position = TargetTransform.position;
            position.y += bottomBefore - bottomAfter;
            if (body != null) body.position = position;
            TargetTransform.position = position;
        }

        Physics2D.SyncTransforms();
    }

    private static float ScaleRatio(float current, float original)
        => Mathf.Abs(original) > 0.00001f ? Mathf.Abs(current / original) : 1f;

    private bool TryGetWorldBottom(out float bottom)
    {
        Physics2D.SyncTransforms();
        bottom = float.PositiveInfinity;

        if (IsUsableCollider(bottomAnchorCollider))
        {
            bottom = bottomAnchorCollider.bounds.min.y;
            return true;
        }

        var colliders = UnityEngine.Pool.ListPool<Collider2D>.Get();
        try
        {
            TargetTransform.GetComponentsInChildren(false, colliders);
            foreach (Collider2D shape in colliders)
                if (IsUsableCollider(shape)) bottom = Mathf.Min(bottom, shape.bounds.min.y);
        }
        finally
        {
            UnityEngine.Pool.ListPool<Collider2D>.Release(colliders);
        }

        if (!float.IsPositiveInfinity(bottom)) return true;

        var renderers = UnityEngine.Pool.ListPool<SpriteRenderer>.Get();
        try
        {
            TargetTransform.GetComponentsInChildren(false, renderers);
            foreach (SpriteRenderer renderer in renderers)
                if (renderer.enabled && renderer.sprite != null && renderer.gameObject.activeInHierarchy)
                    bottom = Mathf.Min(bottom, renderer.bounds.min.y);
        }
        finally
        {
            UnityEngine.Pool.ListPool<SpriteRenderer>.Release(renderers);
        }

        return !float.IsPositiveInfinity(bottom);
    }

    private bool IsUsableCollider(Collider2D shape)
        => shape != null && shape.enabled && !shape.isTrigger && shape.gameObject.activeInHierarchy
            && (shape.transform == TargetTransform || shape.transform.IsChildOf(TargetTransform))
            && (shape.attachedRigidbody == null || shape.attachedRigidbody.simulated)
            && shape.bounds.size.y > 0.00001f;
}
