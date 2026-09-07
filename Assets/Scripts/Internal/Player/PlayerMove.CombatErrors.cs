using UnityEngine;

/// <summary>PlayerMove의 전투 오류 버프·타이머 구현 부분입니다. 별도 컴포넌트가 아니므로 직접 부착하지 않습니다.</summary>
public partial class PlayerMove
{
    private void UpdateCombatErrorTimers()
    {
        if (playerErrorTimer > 0f)
        {
            playerErrorTimer -= Time.deltaTime;
            if (playerErrorTimer <= 0f)
            {
                playerErrorTimer = 0f;
                Debug.Log("거대화 오류 버프 종료");
            }
        }

        if (accelerationAttackTimer > 0f)
        {
            accelerationAttackTimer -= Time.deltaTime;
            if (accelerationAttackTimer <= 0f)
            {
                accelerationAttackTimer = 0f;
                activeAttackSpeedMultiplier = 1f;
                Debug.Log("가속 오류 전투 Paste 종료");
            }
        }

        if (reflectionCombatTimer > 0f)
        {
            reflectionCombatTimer -= Time.deltaTime;
            if (reflectionCombatTimer <= 0f)
            {
                reflectionCombatTimer = 0f;
                Debug.Log("반사 오류 전투 Paste 종료");
            }
        }
    }

    private void TryActivateStoredCombatError()
    {
        StoredErrorType selectedError = GetSelectedStoredErrorType();

        if (selectedError == StoredErrorType.Giant)
        {
            TryActivatePlayerErrorBuff();
            return;
        }

        if (selectedError == StoredErrorType.Reflection)
        {
            reflectionCombatTimer = reflectionCombatDuration;
            ClearStoredReflectionError();
            Debug.Log($"반사 오류를 전투 기술에 Paste했습니다. {reflectionCombatDuration}초 동안 받은 피해를 공격자에게 되돌립니다.");
            return;
        }

        if (selectedError != StoredErrorType.Acceleration)
        {
            Debug.Log("전투 기술에 Paste할 오류가 없습니다.");
            return;
        }

        if (accelerationAttackTimer > 0f)
        {
            Debug.Log("이미 전투 기술에 가속 오류가 적용되어 있습니다.");
            return;
        }

        activeAttackSpeedMultiplier = Mathf.Max(1f, storedErrors.GetMultiplier(StoredErrorType.Acceleration));
        accelerationAttackTimer = accelerationAttackDuration;
        ClearStoredAccelerationError();
        Debug.Log($"가속 오류를 전투 기술에 Paste했습니다. 공격 속도 {activeAttackSpeedMultiplier}배, {accelerationAttackDuration}초 유지");
    }

    private void TryActivatePlayerErrorBuff()
    {
        if (isEditMode)
        {
            Debug.Log("편집 모드에서는 오류를 저장하지 않고, 일반 상태에서만 Q로 버프를 사용할 수 있습니다.");
            return;
        }

        if (!storedErrors.Contains(StoredErrorType.Giant))
        {
            Debug.Log("전투 기술에 Paste할 거대화 오류가 없습니다.");
            return;
        }

        if (playerErrorTimer > 0f)
        {
            Debug.Log("이미 거대화 오류 버프가 활성화 중입니다.");
            return;
        }

        storedPlayerErrorMultiplier = playerErrorEffectMultiplier;
        playerErrorTimer = playerErrorDuration;
        ClearEnvironmentError();

        Debug.Log("거대화 오류를 전투 기술에 Paste했습니다. 공격 범위 " + storedPlayerErrorMultiplier + "배 증가, 지속 시간 " + playerErrorDuration + "초. 보관함에서 오류가 제거되었습니다.");
    }

    private bool IsAnyCombatErrorActive()
    {
        return playerErrorTimer > 0f
            || accelerationAttackTimer > 0f
            || reflectionCombatTimer > 0f;
    }

    private string GetActiveCombatErrorDisplayName()
    {
        if (playerErrorTimer > 0f)
        {
            return "[거대화]";
        }

        if (accelerationAttackTimer > 0f)
        {
            return "[가속]";
        }

        if (reflectionCombatTimer > 0f)
        {
            return "[반사]";
        }

        return "[오류]";
    }
}
