using System.Collections.Generic;
using UnityEngine;

/// <summary>공격 1회별 랜덤 오류와 패링 보상. partial이므로 직접 부착하지 않습니다.</summary>
public sealed partial class TutorialBoss
{
    [Header("패턴 랜덤 오류 (근접 / 낙석 / 충격파)")]
    [SerializeField, Tooltip("근접/낙석/충격파 시작마다 새 오류를 추첨합니다. 같은 회차를 패링하면 해당 회차의 오류를 제거하고 한 번 획득합니다.")] private bool enablePatternErrors = true;
    [SerializeField, Range(0f, 1f), Tooltip("공격 1회에 오류가 붙을 확률입니다. 1이면 매번, 0이면 일반 공격입니다.")] private float patternErrorChance = 1f;
    [Header("2페이즈 / 복합 오류")]
    [SerializeField, Range(.05f,.95f), Tooltip("이 체력 비율 이하에서 2페이즈입니다. 기본 0.5 = HP 50%. 새로 시작하는 패턴부터 복합 오류를 추첨하며 이미 진행 중인 패턴은 유지합니다.")]
    private float secondPhaseHealthRatio = .5f;
    [SerializeField, Range(2,3), Tooltip("2페이즈에서 오류가 붙은 패턴당 서로 다른 오류 수입니다. 기본 2개, 최대 3개. 허용된 오류 종류가 부족하면 가능한 수만 적용합니다.")]
    private int secondPhaseErrorCount = 2;
    public bool IsSecondPhase => health > 0f && health <= Mathf.Max(1f,maxHealth) * Mathf.Clamp(secondPhaseHealthRatio,.05f,.95f);
    [SerializeField, Tooltip("거대화: 근접 보스/타격 범위, 낙석 돌/경고/판정, 충격파 그림/판정을 함께 확대합니다.")] private bool allowGiantPattern = true;
    [SerializeField, Tooltip("가속: 근접 공격 동작, 돌의 낙하 속도, 충격파 이동 속도를 높입니다. 낙석 경고/순차 간격과 충격파 준비 시간은 그대로입니다.")] private bool allowAccelerationPattern = true;
    [SerializeField, Tooltip("반사: 추첨된 시점부터 Pattern Reflection Duration 동안 보스를 공격한 플레이어에게 피해를 돌려줍니다. 해당 회차 패링으로 조기 해제할 수 있습니다.")] private bool allowReflectionPattern = true;
    [SerializeField, Min(.1f), Tooltip("반사 오류 지속 시간(게임 시간, 초)입니다. 동작/낙석/충격파가 끝나도 남은 시간 동안 유지하며 해당 회차 패링, 경직, 사망, 방 이탈 시 일찍 해제됩니다. 시간 만료로는 오류를 지급하지 않습니다.")] private float patternReflectionDuration = 5f;
    [SerializeField, Min(1f), Tooltip("거대화 배율입니다. 패링 보관함에도 같은 배율을 전달합니다.")] private float patternGiantMultiplier = 1.5f;
    [SerializeField, Min(1f), Tooltip("가속 배율입니다. 타격 프레임과 패링 판정은 여전히 애니메이션을 따릅니다.")] private float patternAccelerationMultiplier = 1.5f;
    [SerializeField, Min(0f), Tooltip("보스가 받은 피해를 플레이어에게 돌려주는 비율입니다. 1이면 100%. 보스도 원래 피해를 받으며 재반사/동시 패턴 배율 중첩은 하지 않습니다.")] private float patternReflectionMultiplier = 1f;

    // 다음 공격이 시작되어도 이전 충격파는 발사 당시의 문맥을 보유합니다.
    // 같은 회차의 돌/충격파는 복합 오류 묶음을 공유합니다. 해제/획득은 회차당 한 번입니다.
    private enum PatternKind { Melee, Rockfall, Shockwave }
    private sealed class PatternError
    {
        public readonly Dictionary<StoredErrorType,float> errors = new Dictionary<StoredErrorType,float>(3);
        public bool Has(StoredErrorType type) => !removed && errors.ContainsKey(type);
        public float Multiplier(StoredErrorType type) => !removed && errors.TryGetValue(type,out float value) ? value : 1f;
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
    private float PatternSize => activePatternError != null && activePatternError.kind == PatternKind.Melee
        ? activePatternError.Multiplier(StoredErrorType.Giant) : 1f;
    private float PatternSpeed => activePatternError != null && activePatternError.kind == PatternKind.Melee
        ? activePatternError.Multiplier(StoredErrorType.Acceleration) : 1f;

    private void BeginPatternError(PatternKind kind)
    {
        EndActivePatternError();
        activePatternError = CreatePatternError(kind);
        if (kind == PatternKind.Melee && activePatternError.Has(StoredErrorType.Giant))
        {
            activePatternError.originalScale = transform.localScale;
            activePatternError.scaledBody = true;
            SetPatternBodyScale(transform.localScale * activePatternError.Multiplier(StoredErrorType.Giant));
        }
    }

    private PatternError CreatePatternError(PatternKind kind)
    {
        var pattern = new PatternError { kind = kind };
        var first = RollPatternError();
        if (first != StoredErrorType.None)
        {
            pattern.errors.Add(first,ErrorMultiplier(first));
            var remaining = new List<StoredErrorType>(3);
            if (allowGiantPattern && first != StoredErrorType.Giant) remaining.Add(StoredErrorType.Giant);
            if (allowAccelerationPattern && first != StoredErrorType.Acceleration) remaining.Add(StoredErrorType.Acceleration);
            if (allowReflectionPattern && first != StoredErrorType.Reflection) remaining.Add(StoredErrorType.Reflection);
            int wanted = IsSecondPhase ? Mathf.Clamp(secondPhaseErrorCount,2,3) : 1;
            while (pattern.errors.Count < wanted && remaining.Count > 0)
            {
                int index = Random.Range(0,remaining.Count);
                var type = remaining[index];remaining.RemoveAt(index);
                pattern.errors.Add(type,ErrorMultiplier(type));
            }
        }
        if (pattern.Has(StoredErrorType.Reflection)) pattern.reflectionRemaining = Mathf.Max(.1f,patternReflectionDuration);
        livePatternErrors.Add(pattern);
        return pattern;
    }

    private float ErrorMultiplier(StoredErrorType type) => type switch
    {
        StoredErrorType.Giant => Mathf.Max(1f,patternGiantMultiplier),
        StoredErrorType.Acceleration => Mathf.Max(1f,patternAccelerationMultiplier),
        StoredErrorType.Reflection => Mathf.Max(0f,patternReflectionMultiplier),
        _ => 1f
    };

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
            if (pattern == activePatternError || pattern == rockfallPatternError || pattern == concurrentShockwaveError) continue;
            if (pattern.Has(StoredErrorType.Reflection) && pattern.reflectionRemaining > 0f) continue;
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
            if (!pattern.Has(StoredErrorType.Reflection)) continue;
            pattern.reflectionRemaining = Mathf.Max(0f,pattern.reflectionRemaining-dt);
            // 반사 시간 만료가 같은 회차의 거대화/가속까지 제거해서는 안 됩니다.
            if (pattern.reflectionRemaining <= 0f) pattern.errors.Remove(StoredErrorType.Reflection);
        }
        PrunePatternErrors();
    }

    private void ClearAllPatternErrors()
    {
        foreach (var pattern in livePatternErrors) DeactivatePatternError(pattern);
        livePatternErrors.Clear();
        concurrentShockwaveError = null;concurrentShockwaveWindup = 0f;
    }

    private PatternError FindReflectingPattern()
    {
        PrunePatternErrors();
        PatternError strongest = null;
        foreach (var pattern in livePatternErrors)
            if (pattern.Has(StoredErrorType.Reflection)
                && (strongest == null || pattern.Multiplier(StoredErrorType.Reflection) > strongest.Multiplier(StoredErrorType.Reflection))) strongest = pattern;
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
        if (pattern == null || pattern.removed || pattern.errors.Count == 0) return;
        DeactivatePatternError(pattern); // 보상/경직 콜백보다 먼저 잠가 중복 지급을 막습니다.
        if (recipient != null)
            foreach (var reward in pattern.errors) recipient.ReceiveBossPatternError(reward.Key,reward.Value);
    }

    // 시간 만료/전투 취소는 효과만 해제합니다. 패링 보상은 위 경로에서만 지급합니다.
    private void DeactivatePatternError(PatternError pattern)
    {
        if (pattern == null || pattern.removed) return;
        float speed = pattern.Multiplier(StoredErrorType.Acceleration);
        pattern.removed = true;
        pattern.reflectionRemaining = 0f;
        if (pattern == activePatternError)
        {
            RestorePatternBody(pattern);
            if (pattern.kind == PatternKind.Melee && speed > 1f && attackAnimationActive && bossAnimator != null)
                bossAnimator.speed /= speed;
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
        if (pattern.removed || pattern.errors.Count == 0) return;
        shot.SetPatternModifiers(pattern.Multiplier(StoredErrorType.Giant),
            pattern.Multiplier(StoredErrorType.Acceleration),
            false, 0);
        if (shot.TryGetComponent<SpriteRenderer>(out var visual)) visual.color = CompositePatternColor(pattern);
    }

    private void ReflectPatternDamage(float amount, GameObject source, bool canReflect)
    {
        if (!canReflect || source == null || source == gameObject) return;
        var attacker = source.GetComponentInParent<PlayerMove>();
        if (attacker == null) return;
        var pattern = FindReflectingPattern();
        if (pattern != null) CombatDamageUtility.TryApplyDamage(attacker.gameObject, amount * pattern.Multiplier(StoredErrorType.Reflection), gameObject, false);
    }

    private static string PatternLabel(PatternError pattern)
    {
        if (pattern == null || pattern.removed || pattern.errors.Count == 0) return "오류 없음";
        var names = new List<string>(3);
        foreach (var error in pattern.errors) names.Add(ErrorRules.DisplayName(error.Key));
        return string.Join(" + ",names);
    }

    private static Color CompositePatternColor(PatternError pattern)
    {
        if (pattern == null || pattern.removed || pattern.errors.Count == 0) return Color.white;
        Color color = Color.clear;
        foreach (var error in pattern.errors) color += PatternColor(error.Key);
        return color / pattern.errors.Count;
    }

    private static Color PatternColor(StoredErrorType type) => type switch
    {
        StoredErrorType.Giant => new Color(1f, .65f, .25f, 1f),
        StoredErrorType.Acceleration => new Color(.6f, 1f, .35f, 1f),
        StoredErrorType.Reflection => new Color(.9f, .5f, 1f, 1f),
        _ => Color.white
    };
}
