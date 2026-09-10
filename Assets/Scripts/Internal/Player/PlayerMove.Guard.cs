using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PlayerMove의 우클릭 유지 가드와 방향별 애니메이션을 처리합니다.
/// partial 구현 파일이므로 GameObject에 따로 부착하지 않습니다.
/// 피해 감소율은 PlayerMove Inspector에서 설정하고 프레임은 Guard_*.anim에 직접 넣습니다.
/// </summary>
public partial class PlayerMove
{
    private static readonly int GuardRightStateHash = Animator.StringToHash("Guard_Right");
    private static readonly int GuardUpStateHash = Animator.StringToHash("Guard_Up");
    private static readonly int GuardDownStateHash = Animator.StringToHash("Guard_Down");
    private bool isGuarding;
    private bool guardHasFocus = true;
    private int currentGuardStateHash;
    private bool isGuardFrameHeld;
    private float guardHoldNormalizedTime = -1f;
    private const float GuardGaugeCostPerHit = 10f;
    private float currentGuardGauge;
    private bool guardRequiresRelease;

    private void UpdateGuardGauge(float deltaTime)
    {
        float maximum = Mathf.Max(1f, maxGuardGauge);
        currentGuardGauge = Mathf.Clamp(currentGuardGauge, 0f, maximum);
        if (!isGuarding && !IsGuardRequested())
            currentGuardGauge = Mathf.Min(maximum, currentGuardGauge
                + Mathf.Max(0f, guardGaugeRecoveryPerSecond) * Mathf.Max(0f, deltaTime));
    }

    private void ConsumeGuardGaugeOnHit()
    {
        currentGuardGauge = Mathf.Max(0f, currentGuardGauge - GuardGaugeCostPerHit);
        if (currentGuardGauge > 0f) return;

        // 회복 직후, 계속 누르고 있던 우클릭으로 자동 재가드되는 것을 방지합니다.
        guardRequiresRelease = true;
        EndGuard();
        Debug.Log("가드 게이지 소진: 가드 해제. 우클릭을 놓고 게이지 회복 후 다시 누르세요.", this);
    }

    private void DrawGuardGauge()
    {
        if (!showGuardGauge || !Application.isPlaying) return;
        float maximum = Mathf.Max(1f, maxGuardGauge);
        float y = Mathf.Max(20f, Screen.height - 86f);
        GUI.Box(new Rect(20f, y, 260f, 66f), GUIContent.none);
        string status = isGuarding ? "가드 중" : guardRequiresRelease ? "가드 해제 · 우클릭 놓기" : "회복 / 대기";
        GUI.Label(new Rect(30f, y + 5f, 240f, 22f), $"가드 {currentGuardGauge:0.0} / {maximum:0}  {status}");
        Color previousColor = GUI.color;
        GUI.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        GUI.DrawTexture(new Rect(30f, y + 34f, 240f, 16f), Texture2D.whiteTexture);
        GUI.color = isGuarding ? new Color(0.3f, 0.75f, 1f) : new Color(0.4f, 0.85f, 0.5f);
        GUI.DrawTexture(new Rect(30f, y + 34f, 240f * Mathf.Clamp01(currentGuardGauge / maximum), 16f), Texture2D.whiteTexture);
        GUI.color = previousColor;
    }

    // 편집 모드의 입력에는 간섭하지 않습니다. 공격 도중 가드로 공격을 끊을 수 없습니다.
    private bool IsGuardRequested()
    {
        return isActiveAndEnabled && guardHasFocus && !isEditMode && !isAttacking
            && currentGuardGauge > 0f && !guardRequiresRelease
            && Mouse.current != null && Mouse.current.rightButton.isPressed;
    }

    private void UpdateGuardState()
    {
        if (Mouse.current != null && !Mouse.current.rightButton.isPressed)
            guardRequiresRelease = false;

        if (isPlayingParry)
        {
            if (isEditMode || !guardHasFocus || guardRequiresRelease || currentGuardGauge <= 0f)
                EndGuard();
            else
            {
                // 방어 여부는 현재 입력을 따르지만, 성공 연출은 우클릭 해제로 끊지 않습니다.
                isGuarding = IsGuardRequested();
                return;
            }
        }

        if (!IsGuardRequested())
        {
            EndGuard();
            return;
        }

        Vector2 direction = GetCardinalAttackDirection(GetMouseDirection());
        int animationDirection = direction.y != 0f ? (direction.y > 0f ? 0 : 1) : 2;
        int stateHash = animationDirection == 0 ? GuardUpStateHash
            : animationDirection == 1 ? GuardDownStateHash : GuardRightStateHash;

        animator.SetBool(IsMovingHash, false);
        animator.SetInteger(DirectionHash, animationDirection);
        spriteRenderer.flipX = animationDirection == 2 && direction.x < 0f;

        // 같은 방향은 재시작하지 않고, 방향별 클립의 끝에서 세 번째 프레임을 유지합니다.
        if (!isGuarding || currentGuardStateHash != stateHash)
        {
            isGuardFrameHeld = false;
            guardHoldNormalizedTime = -1f;
            animator.speed = baseAnimatorSpeed;
            animator.Play(stateHash, 0, 0f);
        }

        animator.speed = isGuardFrameHeld ? 0f : baseAnimatorSpeed;

        currentGuardStateHash = stateHash;
        isGuarding = true;
    }

    private void LateUpdate()
    {
        UpdateParryFeedbackImage();
        if (isPlayingParry)
        {
            UpdateParryAnimation();
            return;
        }
        if (!isGuarding || isGuardFrameHeld || animator == null || !IsGuardRequested()) return;

        // Animator 평가 후, 화면에 그리기 전에 목표 프레임으로 보정합니다.
        // 저프레임으로 목표 시점을 지나쳤더라도 끝의 두 프레임이 화면에 나오지 않습니다.
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.shortNameHash != currentGuardStateHash) return;

        if (guardHoldNormalizedTime < 0f)
        {
            AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
            if (clips.Length == 0 || clips[0].clip == null) return;
            AnimationClip clip = clips[0].clip;
            guardHoldNormalizedTime = clip.length > 0f && clip.frameRate > 0f
                ? AttackMath.ImpactTime(clip.length, clip.frameRate) : 0f;
        }

        if (state.normalizedTime < guardHoldNormalizedTime) return;
        isGuardFrameHeld = true;
        animator.speed = 0f;
        // 부동소수점 오차로 직전 이미지가 선택되지 않도록 프레임 경계 안쪽을 샘플링합니다.
        animator.Play(currentGuardStateHash, 0, Mathf.Clamp01(guardHoldNormalizedTime + 0.00001f));
        animator.Update(0f);
    }

    private void EndGuard()
    {
        parryAvailable = false;
        CancelParryAnimation();
        if (!isGuarding) return;
        isGuarding = false;
        currentGuardStateHash = 0;
        isGuardFrameHeld = false;
        guardHoldNormalizedTime = -1f;
        if (animator != null)
        {
            animator.speed = baseAnimatorSpeed;
            animator.SetBool(IsMovingHash, false);
            animator.Play("Player_Idle", 0, 0f);
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        guardHasFocus = hasFocus;
        if (!hasFocus) EndGuard();
    }

    private void OnDisable()
    {
        parryFeedbackUntil = 0f;
        if (parryFeedbackCanvas != null) parryFeedbackCanvas.gameObject.SetActive(false);
        EndGuard();
    }
}
