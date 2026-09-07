using UnityEngine;

/// <summary>
/// 반사 오류의 활성 상태와 시각 표시를 관리합니다.
/// 반사 적 또는 반사 오류 원본에 직접 부착하거나, 환경 Paste 시 PlayerMove가 물체·표면에 자동으로 추가합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class ReflectionErrorEffect : MonoBehaviour, IErrorSource
{
    [Header("반사 오류")]
    [Tooltip("씬 시작과 동시에 반사 오류를 활성화합니다. 반사 오류 원본에는 켜고, Paste 대상에는 끕니다.")]
    [SerializeField] private bool triggerOnStart = true;

    [Tooltip("반사 오류가 활성화된 대상을 표시할 색입니다. Cut하면 원래 색으로 돌아갑니다.")]
    [SerializeField] private Color reflectedTint = new Color(0.35f, 0.9f, 1f, 1f);

    private SpriteRenderer targetRenderer;
    private Color baseColor = Color.white;

    private ErrorEffectState state;
    public bool IsActive => state.IsActive;
    public bool IsPasted => state.IsPasted;
    public bool CanCut => state.CanCut;
    public StoredErrorType ErrorType => StoredErrorType.Reflection;
    public float StoredMultiplier => 0f;
    public void RemoveError() => ResetReflection();

    public static bool CanPasteTo(PasteTargetType targetType) => ErrorRules.CanPasteTo(StoredErrorType.Reflection, targetType);

    private void Awake()
    {
        targetRenderer = GetComponent<SpriteRenderer>();

        if (targetRenderer != null)
        {
            baseColor = targetRenderer.color;
        }

        if (triggerOnStart)
        {
            Trigger();
        }
    }

    public void Trigger()
    {
        state.ActivateOriginal();

        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
            if (targetRenderer != null)
            {
                baseColor = targetRenderer.color;
            }
        }

        if (targetRenderer != null)
        {
            targetRenderer.color = reflectedTint;
        }
    }

    /// <summary>붙여넣기로 적용한 오류는 다시 Cut할 수 없습니다.</summary>
    public void ApplyPaste()
    {
        Trigger();
        state.MarkPasted();
    }

    public void ResetReflection()
    {
        state.Deactivate();

        if (targetRenderer != null)
        {
            targetRenderer.color = baseColor;
        }
    }
}
