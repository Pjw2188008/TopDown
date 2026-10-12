using System;
using UnityEngine;

/// <summary>튜토리얼 재도전용 보관함·자원 스냅샷. PlayerMove partial이며 직접 부착하지 않습니다.</summary>
public partial class PlayerMove
{
    internal Action CaptureTutorialState()
    {
        var inventory = storedErrors.Copy();
        float giantTime = playerErrorTimer, giantMultiplier = storedPlayerErrorMultiplier;
        float accelerationTime = accelerationAttackTimer, speedMultiplier = activeAttackSpeedMultiplier;
        float reflectionTime = reflectionCombatTimer;
        float stamina = currentDashStamina, guard = currentGuardGauge;
        Vector2 direction = lastDirection;
        bool flip = spriteRenderer != null && spriteRenderer.flipX;
        return () =>
        {
            if (this == null) return;
            storedErrors.RestoreFrom(inventory);
            playerErrorTimer = giantTime;
            storedPlayerErrorMultiplier = giantMultiplier;
            accelerationAttackTimer = accelerationTime;
            activeAttackSpeedMultiplier = speedMultiplier;
            reflectionCombatTimer = reflectionTime;
            currentDashStamina = Mathf.Clamp(stamina, 0f, Mathf.Max(1f, maxDashStamina));
            currentGuardGauge = Mathf.Clamp(guard, 0f, Mathf.Max(1f, maxGuardGauge));
            dashCooldownRemaining = dashRecoveryRemaining = 0f;
            dashFeedbackUntil = 0f;
            nextAttackTime = 0f;
            attackImpactTriggered = true;
            guardRequiresRelease = false;
            lastDirection = direction;
            if (spriteRenderer != null) spriteRenderer.flipX = flip;
            // 버전은 단조 증가를 유지하되 이전 시도의 성공 내역은 재사용하지 않습니다.
            successfulCutsByTarget.Clear();
            successfulEnvironmentPastes.Clear();
            LastSuccessfulCutNames = string.Empty;
            selectedStoredErrorIndex = 0;
            RefreshStoredErrorSelection();
        };
    }
}
