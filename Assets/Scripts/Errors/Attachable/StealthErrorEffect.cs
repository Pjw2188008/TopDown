using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>은신 오류 원본/환경 Paste/시간제 전투 Paste와 노이즈 그림을 관리합니다. Collider와 피해 판정은 바꾸지 않습니다.</summary>
[DisallowMultipleComponent]
public sealed class StealthErrorEffect : MonoBehaviour, IErrorSource
{
    [SerializeField, Tooltip("씬 시작 시 은신 원본으로 활성화합니다. Cut 대상은 Layer를 Error로 지정하세요.")]
    private bool triggerOnStart = true;
    [SerializeField, Range(.02f,1f), Tooltip("은신 중 보이는 정도입니다. 0에 가까울수록 희미합니다.")]
    private float visibleOpacity = .22f;
    [SerializeField, Range(0f,1f), Tooltip("노이즈 밝기와 투명도 변화의 강도입니다.")]
    private float noiseStrength = .35f;
    [SerializeField, Min(1f), Tooltip("노이즈 한 칸의 화면 픽셀 크기입니다.")]
    private float noisePixelSize = 3f;
    [SerializeField, Min(0f), Tooltip("초당 노이즈 모양 변경 횟수입니다. 게임 시간 기준입니다.")]
    private float noiseRate = 12f;
    [SerializeField, Tooltip("은신을 표시할 SpriteRenderer입니다. 비우면 자신/자식의 그림을 사용합니다.")]
    private SpriteRenderer[] targetRenderers;

    private struct VisualState
    {
        public SpriteRenderer renderer;
        public Material original;
        public float originalAlpha;
    }
    private readonly List<VisualState> visuals = new List<VisualState>();
    private ErrorEffectState state;
    private Material stealthMaterial;
    private float remainingDuration = -1f;
    private bool awakeCompleted;
    public StoredErrorType ErrorType => StoredErrorType.Stealth;
    public float StoredMultiplier => 1f;
    public bool IsActive => state.IsActive;
    public bool IsPasted => state.IsPasted;
    public bool CanCut => state.CanCut;
    public bool IsTimed => IsActive && remainingDuration > 0f;
    public float RemainingDuration => Mathf.Max(0f,remainingDuration);
    public void RemoveError() => ResetStealth();
    public static bool CanPasteTo(PasteTargetType target) => ErrorRules.CanPasteTo(StoredErrorType.Stealth,target);
    public static bool Conceals(GameObject target)
    {
        if (target == null) return false;
        for (Transform current=target.transform;current!=null;current=current.parent)
            if (current.TryGetComponent<StealthErrorEffect>(out var effect) && effect.isActiveAndEnabled && effect.IsActive) return true;
        return false;
    }
    private void Awake()
    {
        awakeCompleted = true;
        if (triggerOnStart) Trigger();
    }
    private void OnEnable() { if (IsActive) ApplyVisuals(); }
    public void Trigger()
    {
        state.ActivateOriginal();remainingDuration = -1f;
        if (isActiveAndEnabled) ApplyVisuals();
    }
    public void ApplyPaste()
    {
        Trigger();state.MarkPasted();
    }
    public void ApplyCombat(float duration,float opacity,float noise)
    {
        visibleOpacity = Mathf.Clamp(opacity,.02f,1f);noiseStrength = Mathf.Clamp01(noise);
        ApplyPaste();remainingDuration = Mathf.Max(.1f,duration);ConfigureMaterial();
    }
    private void Update() => Advance(Time.deltaTime);
    private void Advance(float deltaTime)
    {
        if (!IsTimed || deltaTime <= 0f) return;
        remainingDuration = Mathf.Max(0f,remainingDuration-deltaTime);
        if (remainingDuration <= 0f) ResetStealth();
    }
    public void ResetStealth()
    {
        state.Deactivate();remainingDuration = -1f;RestoreVisuals();
    }
    private void ApplyVisuals()
    {
        if (visuals.Count > 0) return;
        if (stealthMaterial == null)
        {
            var shader = Resources.Load<Shader>("ErrorEffects/StealthNoise");
            if (shader != null) stealthMaterial = new Material(shader) { name = "Stealth Noise (Runtime)" };
        }
        ConfigureMaterial();
        var renderers = targetRenderers != null && targetRenderers.Length > 0
            ? targetRenderers : GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var renderer in renderers)
        {
            if (renderer == null || visuals.Exists(v => v.renderer == renderer)) continue;
            Color color = SpriteTint.GetColor(renderer);
            visuals.Add(new VisualState { renderer=renderer, original=renderer.sharedMaterial, originalAlpha=color.a });
            if (stealthMaterial != null) renderer.sharedMaterial = stealthMaterial;
            else { color.a *= visibleOpacity;SpriteTint.SetColor(renderer,color); }
        }
    }
    private void ConfigureMaterial()
    {
        if (stealthMaterial == null) return;
        stealthMaterial.SetFloat("_StealthOpacity",visibleOpacity);
        stealthMaterial.SetFloat("_NoiseStrength",noiseStrength);
        stealthMaterial.SetFloat("_NoisePixelSize",Mathf.Max(1f,noisePixelSize));
        stealthMaterial.SetFloat("_NoiseRate",Mathf.Max(0f,noiseRate));
    }
    private void OnValidate() => ConfigureMaterial();
    private void RestoreVisuals()
    {
        foreach (var visual in visuals)
        {
            if (visual.renderer == null) continue;
            if (stealthMaterial != null)
            {
                if (visual.renderer.sharedMaterial == stealthMaterial) visual.renderer.sharedMaterial = visual.original;
            }
            else
            {
                Color color=SpriteTint.GetColor(visual.renderer);color.a=visual.originalAlpha;
                SpriteTint.SetColor(visual.renderer,color);
            }
        }
        visuals.Clear();
    }
    internal Action CaptureTutorialState()
    {
        var savedState = state;float savedTime = remainingDuration;
        if (!awakeCompleted && triggerOnStart) savedState.ActivateOriginal();
        return () =>
        {
            if (this == null) return;
            RestoreVisuals();state=savedState;remainingDuration=savedTime;
            if (IsActive && isActiveAndEnabled) ApplyVisuals();
        };
    }
    private void OnDisable() => RestoreVisuals();
    private void OnDestroy()
    {
        RestoreVisuals();
        if (stealthMaterial != null) Destroy(stealthMaterial);
    }
}
