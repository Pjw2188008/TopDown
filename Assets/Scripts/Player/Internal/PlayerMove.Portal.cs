using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>포탈 V 입력과 안전한 위치 이동. PlayerMove partial이므로 별도 부착하지 않습니다.</summary>
public partial class PlayerMove
{
    private float nextPortalUseTime;
    private int lastPortalUseFrame = -1;
    public bool CanUsePortal => isActiveAndEnabled && guardHasFocus && Time.timeScale > 0f
        && !IsInstantlyDead && currentHealth > 0f && !isErrorCodexOpen && !isReplacingStoredError && !isSelectingStoredError
        && Time.unscaledTime >= nextPortalUseTime && lastPortalUseFrame != Time.frameCount;

    private bool HandlePortalInput()
    {
        if (!CanUsePortal || Keyboard.current == null || !Keyboard.current.vKey.wasPressedThisFrame) return false;
        Portal portal = Portal.FindNearest(this);
        return portal != null && portal.TryTravel(this);
    }

    /// <summary>HP/오류/체크포인트는 유지하고 진행 중 동작만 종료합니다. 사망 재시작과 별개입니다.</summary>
    public bool TravelThroughPortal(Vector3 destination)
    {
        if (!CanUsePortal || !float.IsFinite(destination.x) || !float.IsFinite(destination.y)
            || !float.IsFinite(destination.z)) return false;
        foreach (var boss in FindObjectsByType<TutorialBoss>(FindObjectsSortMode.None))
            if (boss.gameObject.scene == gameObject.scene && boss.BlocksPortalTravel(transform.position,destination)) return false;

        CaptureRespawnPosition();
        lastPortalUseFrame = Time.frameCount;
        nextPortalUseTime = Time.unscaledTime + .25f;
        ReleaseInteraction();
        CancelDash();
        ClearRunAfterimages();
        ResetSideViewJumpHeight();
        EndGuard();
        isAttacking = false;
        environmentPaste.Cancel();
        if (isEditMode) ToggleEditMode();
        parryFeedbackUntil = 0f;
        if (parryFeedbackCanvas != null) parryFeedbackCanvas.gameObject.SetActive(false);
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.speed = baseAnimatorSpeed;
            animator.SetBool(IsMovingHash,false);
            if (animator.HasState(0,Animator.StringToHash("Player_Idle"))) animator.Play("Player_Idle",0,0f);
        }
        if (playerBody == null) EnsurePlayerPhysics();
        playerBody.linearVelocity = Vector2.zero;
        playerBody.angularVelocity = 0f;
        playerBody.position = destination;
        transform.position = destination;
        NotifyHazardTeleport(); // 레이저/튜토리얼의 이동 선분 검사에서 순간이동 경로를 제외합니다.
        Physics2D.SyncTransforms();
        foreach (var cameraFollow in FindObjectsByType<CameraFollow>(FindObjectsSortMode.None))
            cameraFollow.NotifyPortalTravel(transform);
        return true;
    }
}
