using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Space 단발 대쉬, 소모/자동 회복 스태미나, 대쉬 충돌 검사와 HUD입니다.
/// PlayerMove의 partial 구현이므로 별도 부착하지 않습니다. 무적이나 새 애니메이션은 추가하지 않습니다.
/// </summary>
public partial class PlayerMove
{
    private bool isDashing;
    private bool didDashThisFrame;
    private Vector2 dashDirection;
    private float dashRemaining;
    private float dashCooldownRemaining;
    private float currentDashStamina;
    private float dashRecoveryRemaining;
    private float dashFeedbackUntil;
    private readonly List<RaycastHit2D> dashHits = new List<RaycastHit2D>(16);

    private bool TryBeginDash(Vector2 movement)
    {
        if (isDashing || isAttacking || isGuarding || isPlayingParry || IsGuardRequested()
            || !guardHasFocus || isErrorCodexOpen || !isActiveAndEnabled || Time.timeScale <= 0f
            || isSelectingStoredError || isReplacingStoredError || environmentPaste.IsBusy
            || dashCooldownRemaining > 0f || animator == null || spriteRenderer == null) return false;

        float cost = Mathf.Max(1f, dashStaminaCost);
        if (currentDashStamina < cost)
        {
            dashFeedbackUntil = Time.unscaledTime + 0.8f;
            return false;
        }
        Vector2 direction = movement.sqrMagnitude > 0.0001f ? movement : lastDirection;
        dashDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        lastDirection = dashDirection;
        currentDashStamina -= cost;
        dashRemaining = Mathf.Max(0.01f, dashDuration);
        dashRecoveryRemaining = Mathf.Max(0f, dashStaminaRecoveryDelay);
        isDashing = true;
        parryAvailable = false;
        runAfterimageCooldown = 0f;
        return true;
    }

    // 대쉬 중인 프레임에는 일반 이동을 더하지 않습니다. 마지막 프레임도 남은 시간만 이동합니다.
    private bool UpdateDashMovement(float deltaTime)
    {
        if (!isDashing) return false;
        float step = Mathf.Min(Mathf.Max(0f, deltaTime), dashRemaining);
        if (step <= 0f) return true;
        didDashThisFrame = true;
        float wanted = Mathf.Max(0.1f, dashSpeed) * step;
        float distance = GetUnblockedDashDistance(wanted);
        transform.position += (Vector3)(dashDirection * distance);
        didRunThisFrame = distance > 0.00001f;
        runAfterimageDirection = dashDirection;
        dashRemaining = Mathf.Max(0f, dashRemaining - step);
        if (dashRemaining <= 0f || distance < wanted - 0.00001f) EndDash();
        return true;
    }

    private float GetUnblockedDashDistance(float wanted)
    {
        Collider2D body = GetComponent<Collider2D>();
        if (body == null || !body.enabled) return wanted;
        // 이동을 Transform으로 처리하는 기존 구조이므로 Cast 전에 최신 위치를 동기화합니다.
        Physics2D.SyncTransforms();
        var filter = new ContactFilter2D();
        filter.SetLayerMask(dashBlockingLayers.value & Physics2D.GetLayerCollisionMask(gameObject.layer));
        filter.useTriggers = false;
        if (body.attachedRigidbody != null)
            body.Cast(dashDirection, filter, dashHits, wanted + 0.02f);
        else
            // Rigidbody 없는 현재 Transform 이동 구성은 월드 경계 사각형으로 검사합니다.
            Physics2D.BoxCast(body.bounds.center, body.bounds.size, 0f,
                dashDirection, filter, dashHits, wanted + 0.02f);
        float allowed = wanted;
        foreach (RaycastHit2D hit in dashHits)
        {
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform)) continue;
            if (Physics2D.GetIgnoreCollision(body, hit.collider)) continue;
            // 이미 닿은 벽에서 멀어지거나 벽에 평행하게 이동하는 것은 허용합니다.
            if (Vector2.Dot(dashDirection, hit.normal) >= -0.001f) continue;
            allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - 0.02f));
        }
        return allowed;
    }

    private void EndDash()
    {
        isDashing = false;
        dashRemaining = 0f;
        dashCooldownRemaining = Mathf.Max(0f, dashCooldown);
        dashRecoveryRemaining = Mathf.Max(0f, dashStaminaRecoveryDelay);
    }

    private void CancelDash()
    {
        if (isDashing) EndDash();
        didDashThisFrame = false;
        // 중단하더라도 이미 소모한 스태미나는 환불하지 않습니다.
    }

    private void UpdateDashStamina(float deltaTime)
    {
        float dt = Mathf.Max(0f, deltaTime);
        currentDashStamina = Mathf.Clamp(currentDashStamina, 0f, Mathf.Max(1f, maxDashStamina));
        dashCooldownRemaining = Mathf.Max(0f, dashCooldownRemaining - dt);
        if (isDashing) return;
        float recoveryTime = Mathf.Max(0f, dt - dashRecoveryRemaining);
        dashRecoveryRemaining = Mathf.Max(0f, dashRecoveryRemaining - dt);
        currentDashStamina = Mathf.Min(Mathf.Max(1f, maxDashStamina), currentDashStamina
            + recoveryTime * Mathf.Max(0f, dashStaminaRecoveryPerSecond));
    }

    private void DrawDashStamina()
    {
        if (!showDashStamina || !Application.isPlaying) return;
        float maximum = Mathf.Max(1f, maxDashStamina);
        float y = Mathf.Max(20f, Screen.height - 162f);
        GUI.Box(new Rect(20f, y, 260f, 66f), GUIContent.none);
        string status = Time.unscaledTime < dashFeedbackUntil ? "스태미나 부족"
            : isDashing ? "대쉬 중" : "Space · 대쉬";
        GUI.Label(new Rect(30f, y + 5f, 245f, 22f), $"스태미나 {currentDashStamina:0} / {maximum:0}  {status}");
        Color previousColor = GUI.color;
        GUI.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        GUI.DrawTexture(new Rect(30f, y + 34f, 240f, 16f), Texture2D.whiteTexture);
        GUI.color = currentDashStamina < Mathf.Max(1f, dashStaminaCost)
            ? new Color(0.85f, 0.45f, 0.2f) : new Color(0.9f, 0.8f, 0.25f);
        GUI.DrawTexture(new Rect(30f, y + 34f, 240f * Mathf.Clamp01(currentDashStamina / maximum), 16f), Texture2D.whiteTexture);
        GUI.color = previousColor;
    }
}
