using UnityEngine;

public class GiantErrorEffect : MonoBehaviour
{
    [Header("Scale Effect")]
    [SerializeField] private float targetMultiplier = 3f;
    [SerializeField] private float growthSpeed = 1.5f;
    [SerializeField] private bool triggerOnStart = true;
    [SerializeField] private AnimationCurve expansionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Vector3 baseScale;
    private bool isGrowing;
    private float growthProgress;

    public float CurrentMultiplier => targetMultiplier;

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
        isGrowing = true;
        growthProgress = 0f;
    }

    public void ResetScale()
    {
        transform.localScale = baseScale;
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
