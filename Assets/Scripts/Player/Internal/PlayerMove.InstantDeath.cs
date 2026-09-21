using UnityEngine;

/// <summary>레이저 등 즉사 기믹의 시작 위치 복귀입니다. PlayerMove partial이므로 직접 부착하지 않습니다.</summary>
public partial class PlayerMove
{
    [Header("즉사 기믹 / 재시작")]
    [Tooltip("선택적 재시작 지점입니다. 비우면 플레이어가 처음 생성된 위치를 사용합니다. 레이저 밖의 안전한 위치에 두세요.")]
    [SerializeField] private Transform instantDeathRespawnPoint;
    private Vector3 initialRespawnPosition;
    private bool respawnPositionCaptured;
    private int lastInstantDeathFrame = -1;
    public bool IsInstantlyDead => lastInstantDeathFrame == Time.frameCount;
    public int HazardTeleportVersion { get; private set; }

    private void CaptureRespawnPosition()
    {
        if (respawnPositionCaptured) return;
        initialRespawnPosition = transform.position;
        respawnPositionCaptured = true;
    }

    /// <summary>가드/패링/반사와 테스트용 일반 피해 회복 옵션을 거치지 않고 즉사 후 재시작합니다.</summary>
    public bool KillInstantly(GameObject source)
    {
        if (!isActiveAndEnabled || IsInstantlyDead) return false;
        CaptureRespawnPosition();
        lastInstantDeathFrame = Time.frameCount;
        currentHealth = 0f;
        ReleaseInteraction();
        CancelDash();
        ClearRunAfterimages();
        CloseErrorCodex();
        EndGuard();
        parryFeedbackUntil = 0f;
        if (parryFeedbackCanvas != null) parryFeedbackCanvas.gameObject.SetActive(false);
        isAttacking = false;
        environmentPaste.Cancel();
        isSelectingStoredError = false;
        if (isEditMode) ToggleEditMode();
        if (isReplacingStoredError) CancelStoredErrorReplacement("즉사로 오류 교체를 취소했습니다.");
        if (editModeOverlay != null) editModeOverlay.color = Color.clear;
        if (animator != null)
        {
            animator.speed = baseAnimatorSpeed;
            if (animator.runtimeAnimatorController != null)
            {
                animator.SetBool(IsMovingHash, false);
                if (animator.HasState(0, Animator.StringToHash("Player_Idle"))) animator.Play("Player_Idle", 0, 0f);
            }
        }

        Vector3 destination = instantDeathRespawnPoint != null ? instantDeathRespawnPoint.position : initialRespawnPosition;
        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector2.zero;
            playerBody.position = destination;
        }
        transform.position = destination;
        NotifyHazardTeleport();
        Physics2D.SyncTransforms();
        currentHealth = Mathf.Max(1f, maxHealth);
        DamageBlink.Play(gameObject);
        Debug.Log("레이저 즉사: 시작 위치로 돌아가 HP를 회복했습니다.", this);
        return true;
    }

    /// <summary>일반 워프/체크포인트 이동 후에도 호출하면 레이저가 워프 경로를 이동으로 오인하지 않습니다.</summary>
    public void NotifyHazardTeleport() => HazardTeleportVersion++;
}
