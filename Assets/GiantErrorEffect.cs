using UnityEngine;

public class GiantErrorEffect : MonoBehaviour
{
    [Header("거대화 오류")]
    [Tooltip("거대화가 적용된 대상의 최종 크기 배율입니다. Cut하면 이 배율이 오류 정보로 보관됩니다.")]
    [SerializeField] private float targetMultiplier = 3f;

    [Tooltip("원래 크기에서 목표 크기까지 커지는 속도입니다. 값이 높을수록 거대화 연출이 빨리 끝납니다.")]
    [SerializeField] private float growthSpeed = 1.5f;

    [Tooltip("씬 시작과 동시에 거대화 오류를 적용할지 결정합니다. 오류를 받을 Paste 대상이라면 끄는 것을 권장합니다.")]
    [SerializeField] private bool triggerOnStart = true;

    [Tooltip("거대화 진행도에 따른 크기 변화 곡선입니다. 가로축은 시간, 세로축은 적용 비율입니다.")]
    [SerializeField] private AnimationCurve expansionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Vector3 baseScale;
    private bool isGrowing;
    private float growthProgress;

    public float CurrentMultiplier => targetMultiplier;
    public bool IsActive { get; private set; }

    public static bool CanPasteTo(PasteTargetType targetType)
    {
        return targetType == PasteTargetType.Living
            || targetType == PasteTargetType.Object
            || targetType == PasteTargetType.CombatSkill;
    }

    private void Awake()
    {
        baseScale = transform.localScale;

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
        IsActive = true;
        isGrowing = true;
        growthProgress = 0f;
    }

    public void ResetScale()
    {
        transform.localScale = baseScale;
        IsActive = false;
        isGrowing = false;
        growthProgress = 0f;
    }

    private void Update()
    {
        if (!isGrowing)
        {
            return;
        }

        growthProgress += Time.deltaTime * growthSpeed;
        float t = Mathf.Clamp01(growthProgress);
        float eased = expansionCurve.Evaluate(t);

        Vector3 targetScale = baseScale * targetMultiplier;
        transform.localScale = Vector3.Lerp(baseScale, targetScale, eased);

        if (t >= 1f)
        {
            isGrowing = false;
        }
    }
}
