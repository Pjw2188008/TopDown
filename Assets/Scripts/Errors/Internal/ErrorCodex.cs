using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// 발견한 오류 종류만 기록하는 도감 모델과 설명 데이터입니다. GameObject에 부착하지 않습니다.
/// 보관함/실제 오류 효과와 독립적이며, 도감에서 오류를 꺼내거나 소비하지 않습니다.
/// 새 오류 추가 시 Entries와 Description에 추가하고 ErrorRules의 호환 규칙도 정의하세요.
/// </summary>
public sealed class ErrorCodex
{
    public static readonly IReadOnlyList<StoredErrorType> Entries = Array.AsReadOnly(new[]
    {
        StoredErrorType.Giant, StoredErrorType.Reflection, StoredErrorType.Acceleration
    });

    private readonly HashSet<StoredErrorType> discovered = new HashSet<StoredErrorType>();
    public int Count => discovered.Count;
    public bool IsDiscovered(StoredErrorType type) => discovered.Contains(type);

    /// <summary>최초 발견일 때만 true. None/미정의 종류는 등록하지 않습니다.</summary>
    public bool TryDiscover(StoredErrorType type)
    {
        foreach (StoredErrorType entry in Entries)
            if (entry == type) return discovered.Add(type);
        return false;
    }

    public void Clear() => discovered.Clear();

    /// <summary>2D 탑다운 기준 중심 간 거리. 경계 포함, 음수 반경은 0으로 처리합니다.</summary>
    public static bool IsWithinRange(float dx, float dy, float radius)
    {
        radius = Math.Max(0f, radius);
        return dx * dx + dy * dy <= radius * radius;
    }

    public static string Description(StoredErrorType type) => type switch
    {
        StoredErrorType.Giant => "대상의 크기를 비정상적으로 증가시키는 오류입니다.\n\n"
            + "원본에서 Cut하면 대상이 원래 크기로 돌아옵니다. 생명체나 물체에 Paste하면 그 대상이 커집니다.\n\n"
            + "전투 기술에 사용하면 일정 시간 공격 범위와 이펙트가 커집니다.",
        StoredErrorType.Reflection => "투사체나 받은 피해를 되돌리는 오류입니다.\n\n"
            + "물체·표면에 Paste하면 해당 대상에 충돌한 투사체가 튕깁니다. 반사 오류가 있는 적은 받은 공격 피해를 공격자에게 되돌립니다. 패링한 투사체도 예외가 아닙니다.\n\n"
            + "전투 기술에 사용하면 일정 시간 자신이 받은 피해를 공격자에게 되돌립니다.",
        StoredErrorType.Acceleration => "대상의 움직임을 비정상적으로 빠르게 만드는 오류입니다.\n\n"
            + "원본에서 Cut하면 움직임이 정상 속도로 돌아옵니다. 이동 기능을 갖춘 생명체·물체·투사체에 Paste하면 움직임이 빨라집니다.\n\n"
            + "전투 기술에 사용하면 일정 시간 공격 속도가 빨라집니다.",
        _ => "아직 발견하지 않은 오류입니다."
    };

    public static string TargetName(PasteTargetType type) => type switch
    {
        PasteTargetType.Living => "생명체",
        PasteTargetType.Object => "물체",
        PasteTargetType.Projectile => "투사체",
        PasteTargetType.Surface => "표면",
        PasteTargetType.CombatSkill => "전투 기술",
        _ => "알 수 없음"
    };

    /// <summary>도감 표기와 실제 Paste 허용 여부가 어긋나지 않도록 기존 규칙을 사용합니다.</summary>
    public static string Compatibility(StoredErrorType type)
    {
        var text = new StringBuilder();
        foreach (PasteTargetType target in Enum.GetValues(typeof(PasteTargetType)))
            text.Append(ErrorRules.CanPasteTo(type, target) ? "가능  ·  " : "불가  ·  ")
                .Append(TargetName(target)).Append('\n');
        return text.ToString();
    }
}
