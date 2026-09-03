using UnityEngine;

/// <summary>
/// 오류 호환성을 판정할 때 사용하는 Paste 대상 분류입니다.
/// </summary>
public enum PasteTargetType
{
    Living,
    Object,
    Projectile,
    Surface,
    CombatSkill
}

/// <summary>
/// GameObject가 생명체·물체·투사체·표면·전투 기술 중 어떤 Paste 대상인지 표시합니다.
/// 오류를 Paste할 수 있게 만들 대상 GameObject에 직접 부착합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class PasteTarget : MonoBehaviour
{
    [Header("Paste 호환 대상")]
    [Tooltip("이 오브젝트의 대상 분류입니다. 생명체·물체·투사체·표면·전투 기술 중 하나를 선택하며, 각 오류는 이 분류를 기준으로 Paste 가능 여부를 판단합니다.")]
    [SerializeField] private PasteTargetType targetType = PasteTargetType.Object;

    public PasteTargetType TargetType => targetType;
}
