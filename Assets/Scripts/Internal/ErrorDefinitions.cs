/// <summary>오류의 종류와 표시 이름/호환 규칙. GameObject에 부착하지 않습니다.</summary>
public enum StoredErrorType { None = 0, Giant = 1, Acceleration = 2, Reflection = 3 }

/// <summary>Inspector에 저장되는 값이므로 기존 번호를 유지합니다.</summary>
public enum PasteTargetType { Living = 0, Object = 1, Projectile = 2, Surface = 3, CombatSkill = 4 }

public static class ErrorRules
{
    public static string DisplayName(StoredErrorType type) => type switch
    {
        StoredErrorType.Giant => "[거대화]",
        StoredErrorType.Acceleration => "[가속]",
        StoredErrorType.Reflection => "[반사]",
        _ => "[빈 슬롯]"
    };

    public static bool CanPasteTo(StoredErrorType error, PasteTargetType target) => error switch
    {
        StoredErrorType.Giant => target == PasteTargetType.Living || target == PasteTargetType.Object || target == PasteTargetType.CombatSkill,
        StoredErrorType.Acceleration => target == PasteTargetType.Living || target == PasteTargetType.Object || target == PasteTargetType.Projectile || target == PasteTargetType.CombatSkill,
        StoredErrorType.Reflection => target == PasteTargetType.Object || target == PasteTargetType.Surface || target == PasteTargetType.CombatSkill,
        _ => false
    };
}
