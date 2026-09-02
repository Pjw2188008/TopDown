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
    [SerializeField] private PasteTargetType targetType = PasteTargetType.Object;

    public PasteTargetType TargetType => targetType;
}
