using UnityEngine;

/// <summary>
/// 가속 오류로 속도 배율을 적용받을 수 있는 이동 컴포넌트가 구현하는 규약입니다.
/// 인터페이스이므로 GameObject에 직접 부착하지 않습니다.
/// </summary>
public interface IAccelerationTarget
{
    void SetAccelerationMultiplier(float multiplier);
}

/// <summary>
/// 가속 오류의 활성 상태를 관리하고 같은 GameObject의 IAccelerationTarget에 속도 배율을 전달합니다.
/// 가속 오류 원본에 직접 부착하거나, 환경 Paste 시 PlayerMove가 대상에 자동으로 추가합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class AccelerationErrorEffect : MonoBehaviour
{
    [Header("가속 오류")]
    [Tooltip("가속 오류가 적용됐을 때 IAccelerationTarget의 움직임이 빨라지는 배율입니다. Cut하면 이 배율이 오류 정보로 보관됩니다.")]
    [SerializeField, Min(1f)] private float targetMultiplier = 3f;

    [Tooltip("씬 시작과 동시에 가속 오류를 적용할지 결정합니다. 처음부터 가속된 오류 원본에는 켜고, 오류를 받을 Paste 대상에는 끕니다.")]
    [SerializeField] private bool triggerOnStart = true;

    [Tooltip("가속 오류가 활성화된 대상을 표시할 색입니다. 같은 오브젝트의 SpriteRenderer에 적용되며 Cut하면 원래 색으로 돌아갑니다.")]
    [SerializeField] private Color acceleratedTint = new Color(1f, 0.35f, 0.1f, 1f);

    private IAccelerationTarget accelerationTarget;
    private SpriteRenderer targetRenderer;
    private Color baseColor = Color.white;

    public float CurrentMultiplier => targetMultiplier;
    public bool IsActive { get; private set; }

    public static bool CanPasteTo(PasteTargetType targetType)
    {
        return targetType == PasteTargetType.Living
            || targetType == PasteTargetType.Object
            || targetType == PasteTargetType.Projectile
            || targetType == PasteTargetType.CombatSkill;
    }

    private void Awake()
    {
        FindAccelerationTarget();
        targetRenderer = GetComponent<SpriteRenderer>();

        if (targetRenderer != null)
        {
            baseColor = targetRenderer.color;
        }

        if (triggerOnStart)
        {
            Trigger(targetMultiplier);
        }
    }

    public void Trigger(float multiplier)
    {
        targetMultiplier = Mathf.Max(1f, multiplier);
        FindAccelerationTarget();
        IsActive = true;

        if (accelerationTarget != null)
        {
            accelerationTarget.SetAccelerationMultiplier(targetMultiplier);
        }
        else
        {
            Debug.LogWarning($"{name}에는 가속을 적용할 이동 컴포넌트가 없습니다.", this);
        }

        if (targetRenderer != null)
        {
            targetRenderer.color = acceleratedTint;
        }
    }

    public void ResetAcceleration()
    {
        IsActive = false;

        if (accelerationTarget != null)
        {
            accelerationTarget.SetAccelerationMultiplier(1f);
        }

        if (targetRenderer != null)
        {
            targetRenderer.color = baseColor;
        }
    }

    private void FindAccelerationTarget()
    {
        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IAccelerationTarget target)
            {
                accelerationTarget = target;
                return;
            }
        }

        accelerationTarget = null;
    }
}
