using System.Collections.Generic;
using UnityEngine;

/// <summary>공격 1회별 랜덤 오류와 패링 보상. partial이므로 직접 부착하지 않습니다.</summary>
public sealed partial class TutorialBoss
{
    [Header("패턴 랜덤 오류 (근접 / 낙석 / 충격파)")]
    [SerializeField, Tooltip("근접/낙석/충격파 시작마다 새 오류를 추첨합니다. 같은 회차를 패링하면 해당 회차의 오류를 제거하고 한 번 획득합니다.")] private bool enablePatternErrors = true;
    [SerializeField, Range(0f, 1f), Tooltip("공격 1회에 오류가 붙을 확률입니다. 1이면 매번, 0이면 일반 공격입니다.")] private float patternErrorChance = 1f;
    [SerializeField, Tooltip("거대화: 근접 보스/타격 범위, 낙석 돌/경고/판정, 충격파 그림/판정을 함께 확대합니다.")] private bool allowGiantPattern = true;
    [SerializeField, Tooltip("가속: 근접 공격 동작, 돌의 낙하 속도, 충격파 이동 속도를 높입니다. 낙석 경고/순차 간격과 충격파 준비 시간은 그대로입니다.")] private bool allowAccelerationPattern = true;
    [SerializeField, Tooltip("반사: 추첨된 시점부터 Pattern Reflection Duration 동안 보스를 공격한 플레이어에게 피해를 돌려줍니다. 해당 회차 패링으로 조기 해제할 수 있습니다.")] private bool allowReflectionPattern = true;
    [SerializeField, Min(.1f), Tooltip("반사 오류 지속 시간(게임 시간, 초)입니다. 동작/낙석/충격파가 끝나도 남은 시간 동안 유지하며 해당 회차 패링, 경직, 사망, 방 이탈 시 일찍 해제됩니다. 시간 만료로는 오류를 지급하지 않습니다.")] private float patternReflectionDuration = 5f;
    [SerializeField, Min(1f), Tooltip("거대화 배율입니다. 패링 보관함에도 같은 배율을 전달합니다.")] private float patternGiantMultiplier = 1.5f;
    [SerializeField, Min(1f), Tooltip("가속 배율입니다. 타격 프레임과 패링 판정은 여전히 애니메이션을 따릅니다.")] private float patternAccelerationMultiplier = 1.5f;
    [SerializeField, Min(0f), Tooltip("보스가 받은 피해를 플레이어에게 돌려주는 비율입니다. 1이면 100%. 보스도 원래 피해를 받으며 재반사/동시 패턴 배율 중첩은 하지 않습니다.")] private float patternReflectionMultiplier = 1f;

    // 다음 공격이 시작되어도 이전 충격파는 발사 당시의 문맥을 보유합니다.
    // 네 갈래 전체가 하나의 오류를 공유하여 해제/획득은 공격당 한 번만 발생합니다.
    private enum PatternKind { Melee, Rockfall, Shockwave }
    private sealed class PatternError
    {
        public StoredErrorType type;
        public float multiplier;
        public float reflectionRemaining;
        public PatternKind kind;
        public bool removed, scaledBody;
        public Vector3 originalScale;
        public readonly List<ReflectProjectile> shots = new List<ReflectProjectile>(4);
    }
    private PatternError activePatternError;
    private PatternError resolvingContactError;
    private FallingRock resolvingRock;
    private readonly List<PatternError> livePatternErrors = new List<PatternError>();
    private float PatternSize => activePatternError != null && activePatternError.kind == PatternKind.Melee && !activePatternError.removed
        && activePatternError.type == StoredErrorType.Giant ? activePatternError.multiplier : 1f;
    private float PatternSpeed => activePatternError != null && activePatternError.kind == PatternKind.Melee && !activePatternError.removed
        && activePatternError.type == StoredErrorType.Acceleration ? activePatternError.multiplier : 1f;

    private void BeginPatternError(PatternKind kind)
    {
        EndActivePatternError();
        activePatternError = CreatePatternError(kind);
        if (kind == PatternKind.Melee && activePatternError.type == StoredErrorType.Giant)
        {
            activePatternError.originalScale = transform.localScale;
            activePatternError.scaledBody = true;
            SetPatternBodyScale(transform.localScale * activePatternError.multiplier);
        }
    }

    private PatternError CreatePatternError(PatternKind kind)
    {
        var type = RollPatternError();
        var pattern = new PatternError { type = type, kind = kind,
            reflectionRemaining = type == StoredErrorType.Reflection ? Mathf.Max(.1f,patternReflectionDuration) : 0f,
            multiplier = type switch
        {
            StoredErrorType.Giant => Mathf.Max(1f, patternGiantMultiplier),
            StoredErrorType.Acceleration => Mathf.Max(1f, patternAccelerationMultiplier),
            StoredErrorType.Reflection => Mathf.Max(0f, patternReflectionMultiplier),
            _ => 1f
        }};
        livePatternErrors.Add(pattern);
        return pattern;
    }

    private StoredErrorType RollPatternError()
    {
        if (!enablePatternErrors || patternErrorChance <= 0f || (patternErrorChance < 1f && Random.value >= patternErrorChance)) return StoredErrorType.None;
        int count = (allowGiantPattern ? 1 : 0) + (allowAccelerationPattern ? 1 : 0) + (allowReflectionPattern ? 1 : 0);
        if (count == 0) return StoredErrorType.None;
        int index = Random.Range(0, count);
        if (allowGiantPattern && index-- == 0) return StoredErrorType.Giant;
        if (allowAccelerationPattern && index-- == 0) return StoredErrorType.Acceleration;
        return StoredErrorType.Reflection;
    }

    private void EndActivePatternError()
    {
        RestorePatternBody(activePatternError);
        activePatternError = null; // 발사된 충격파의 오류 수명은 별개입니다.
        PrunePatternErrors();
    }

    private void PrunePatternErrors()
    {
        for (int i = livePatternErrors.Count - 1; i >= 0; i--)
        {
            var pattern = livePatternErrors[i];
            if (pattern == activePatternError || pattern == rockfallPatternError) continue;
            if (!pattern.removed && pattern.type == StoredErrorType.Reflection && pattern.reflectionRemaining > 0f) continue;
            bool flying = false;
            if (!pattern.removed && pattern.kind == PatternKind.Shockwave)
                foreach (var shot in pattern.shots)
                    if (shot != null && shot.isActiveAndEnabled) { flying = true; break; }
            if (!flying) livePatternErrors.RemoveAt(i);
        }
    }

    private void TickPatternErrors(float dt)
    {
        if (dt <= 0f) return;
        foreach (var pattern in livePatternErrors)
        {
            if (pattern.removed || pattern.type != StoredErrorType.Reflection) continue;
            pattern.reflectionRemaining = Mathf.Max(0f,pattern.reflectionRemaining-dt);
            if (pattern.reflectionRemaining <= 0f) DeactivatePatternError(pattern);
        }
        PrunePatternErrors();
    }

    private void ClearAllPatternErrors()
    {
        foreach (var pattern in livePatternErrors) DeactivatePatternError(pattern);
        livePatternErrors.Clear();
    }

    private PatternError FindReflectingPattern()
    {
        PrunePatternErrors();
        PatternError strongest = null;
        foreach (var pattern in livePatternErrors)
            if (!pattern.removed && pattern.type == StoredErrorType.Reflection
                && (strongest == null || pattern.multiplier > strongest.multiplier)) strongest = pattern;
        return strongest; // 이전 충격파와 새 패턴이 겹쳐도 같은 피해를 여러 번 반사하지 않습니다.
    }

    private void RestorePatternBody(PatternError pattern)
    {
        if (pattern == null || !pattern.scaledBody) return;
        SetPatternBodyScale(pattern.originalScale);
        pattern.scaledBody = false;
    }

    private void RemovePatternError(PatternError pattern, PlayerMove recipient)
    {
        if (pattern == null || pattern.removed || pattern.type == StoredErrorType.None) return;
        DeactivatePatternError(pattern); // 보상/경직 콜백보다 먼저 잠가 중복 지급을 막습니다.
        if (recipient != null) recipient.ReceiveBossPatternError(pattern.type, pattern.multiplier);
    }

    // 시간 만료/전투 취소는 효과만 해제합니다. 패링 보상은 위 경로에서만 지급합니다.
    private void DeactivatePatternError(PatternError pattern)
    {
        if (pattern == null || pattern.removed) return;
        pattern.removed = true;
        pattern.reflectionRemaining = 0f;
        if (pattern == activePatternError)
        {
            RestorePatternBody(pattern);
            if (pattern.kind == PatternKind.Melee && pattern.type == StoredErrorType.Acceleration && attackAnimationActive && bossAnimator != null)
                bossAnimator.speed /= pattern.multiplier;
        }
        if (pattern.kind == PatternKind.Rockfall) NormalizeRockfallError(pattern);
        foreach (var shot in pattern.shots)
        {
            if (shot == null) continue;
            shot.SetPatternModifiers(1f, 1f, false, 0);
            if (shot.TryGetComponent<SpriteRenderer>(out var visual)) visual.color = shockwaveColor;
        }
    }

    private void ReceiveShockwaveParry(PatternError pattern, PlayerMove recipient)
    {
        if (!isActiveAndEnabled || phase == Phase.Dead || recipient == null) return;
        RemovePatternError(pattern, recipient);
        ReceiveParry(recipient);
    }

    private void BindShockwaveError(ReflectProjectile shot, PatternError pattern)
    {
        if (pattern == null) return;
        pattern.shots.Add(shot);
        shot.ParryReceiver = recipient => { if (this != null) ReceiveShockwaveParry(pattern, recipient); };
        if (pattern.removed || pattern.type == StoredErrorType.None) return;
        shot.SetPatternModifiers(pattern.type == StoredErrorType.Giant ? pattern.multiplier : 1f,
            pattern.type == StoredErrorType.Acceleration ? pattern.multiplier : 1f,
            false, 0);
        if (shot.TryGetComponent<SpriteRenderer>(out var visual)) visual.color = PatternColor(pattern.type);
    }

    private void ReflectPatternDamage(float amount, GameObject source, bool canReflect)
    {
        if (!canReflect || source == null || source == gameObject) return;
        var attacker = source.GetComponentInParent<PlayerMove>();
        if (attacker == null) return;
        var pattern = FindReflectingPattern();
        if (pattern != null) CombatDamageUtility.TryApplyDamage(attacker.gameObject, amount * pattern.multiplier, gameObject, false);
    }

    private static Color PatternColor(StoredErrorType type) => type switch
    {
        StoredErrorType.Giant => new Color(1f, .65f, .25f, 1f),
        StoredErrorType.Acceleration => new Color(.6f, 1f, .35f, 1f),
        StoredErrorType.Reflection => new Color(.9f, .5f, 1f, 1f),
        _ => Color.white
    };
}
