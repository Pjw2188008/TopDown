using UnityEngine;

[DisallowMultipleComponent]
public sealed class ReflectionErrorEffect : MonoBehaviour
{
    [Header("반사 오류")]
    [Tooltip("씬 시작과 동시에 반사 오류를 활성화합니다. 반사 오류 원본에는 켜고, Paste 대상에는 끕니다.")]
    [SerializeField] private bool triggerOnStart = true;

    [Tooltip("반사 오류가 활성화된 대상을 표시할 색입니다. Cut하면 원래 색으로 돌아갑니다.")]
    [SerializeField] private Color reflectedTint = new Color(0.35f, 0.9f, 1f, 1f);

    private SpriteRenderer targetRenderer;
    private Color baseColor = Color.white;

    public bool IsActive { get; private set; }

    public static bool CanPasteTo(PasteTargetType targetType)
    {
        return targetType == PasteTargetType.Object
            || targetType == PasteTargetType.Surface
            || targetType == PasteTargetType.CombatSkill;
    }

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
        IsActive = true;

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

    public void ResetReflection()
    {
        IsActive = false;

        if (targetRenderer != null)
        {
            targetRenderer.color = baseColor;
        }
    }
}
