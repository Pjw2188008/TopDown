using UnityEngine;

public enum PasteTargetType
{
    Living,
    Object,
    Projectile,
    Surface,
    CombatSkill
}

[DisallowMultipleComponent]
public sealed class PasteTarget : MonoBehaviour
{
    [Header("Paste 호환 대상")]
    [Tooltip("이 오브젝트의 대상 분류입니다. 생명체·물체·투사체·표면·전투 기술 중 하나를 선택하며, 각 오류는 이 분류를 기준으로 Paste 가능 여부를 판단합니다.")]
    [SerializeField] private PasteTargetType targetType = PasteTargetType.Object;

    public PasteTargetType TargetType => targetType;
}
