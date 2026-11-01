using UnityEngine;

/// <summary>은신 전투 Paste 설정과 적에게 보이는 상태입니다. 직접 부착하지 않습니다.</summary>
public partial class PlayerMove
{
    [Header("전투 기술 - 은신 오류")]
    [SerializeField, Min(.1f), Tooltip("은신 전투 Paste의 유지 시간(게임 시간, 초)입니다. 공격/피격으로 해제되지 않으며 무적을 제공하지 않습니다.")]
    private float stealthCombatDuration = 5f;
    [SerializeField, Range(.02f,1f), Tooltip("은신한 플레이어가 희미하게 보이는 정도입니다.")]
    private float stealthCombatOpacity = .18f;
    [SerializeField, Range(0f,1f), Tooltip("플레이어 은신 노이즈의 강도입니다.")]
    private float stealthCombatNoiseStrength = .4f;
    public bool IsHiddenFromEnemies => StealthErrorEffect.Conceals(gameObject);
    private bool IsCombatStealthActive
        => TryGetComponent<StealthErrorEffect>(out var effect) && effect.isActiveAndEnabled && effect.IsTimed;
    private void ActivateCombatStealth()
    {
        if (isEditMode || !storedErrors.Contains(StoredErrorType.Stealth) || IsAnyCombatErrorActive()) return;
        var effect = GetComponent<StealthErrorEffect>();
        if (effect == null) effect = gameObject.AddComponent<StealthErrorEffect>();
        effect.enabled = true;
        effect.ApplyCombat(stealthCombatDuration,stealthCombatOpacity,stealthCombatNoiseStrength);
        DiscardStoredError(StoredErrorType.Stealth);
        ClearRunAfterimages();
        Debug.Log($"은신 전투 Paste: {stealthCombatDuration}초 동안 적의 탐지/추적을 막습니다. 피격 판정은 유지됩니다.",this);
    }
    private void EndCombatStealth()
    {
        if (TryGetComponent<StealthErrorEffect>(out var effect) && effect.IsTimed) effect.ResetStealth();
    }
}
