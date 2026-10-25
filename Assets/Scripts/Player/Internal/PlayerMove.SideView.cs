using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>사이드뷰 전용 중력, 접지 검사와 단발 점프입니다. PlayerMove partial이므로 따로 붙이지 않습니다.</summary>
public partial class PlayerMove
{
    [SerializeField, Min(0f), Tooltip("사이드뷰 하강 가속도입니다. Rigidbody Gravity Scale 대신 이동 전 충돌 검사로 적용합니다.")]
    private float sideViewGravity = 30f;
    [SerializeField, Min(.1f), Tooltip("사이드뷰 최대 낙하 속도입니다.")]
    private float sideViewMaxFallSpeed = 20f;
    [SerializeField, Min(0f), Tooltip("점프 키를 계속 눌렀을 때의 최대 점프 높이(월드 단위)입니다. 0이면 점프를 끕니다. 중력이 0일 때도 점프하지 않습니다.")]
    private float sideViewJumpHeight = 2f;
    [SerializeField, Tooltip("상승 중 점프 키를 놓으면 상승 속도를 줄여 짧게 점프합니다. 계속 누르면 최대 높이까지 뜁니다. 다시 누르더라도 공중에서 상승력을 되돌리지 않습니다.")]
    private bool sideViewVariableJump = true;
    [SerializeField, Range(0f, 1f), Tooltip("점프 키를 놓은 순간 남길 상승 속도의 비율입니다. 낮을수록 짧은 점프가 낮고 빠르게 꺾입니다. 0은 즉시 상승 종료, 1은 기존 고정 높이 점프와 같습니다.")]
    private float sideViewJumpReleaseMultiplier = .45f;
    [SerializeField, Tooltip("사이드뷰 점프 키입니다. 기본 Space이며 누르는 순간에만 점프합니다. 대시 키와 다르게 설정하세요.")]
    private Key sideViewJumpKey = Key.Space;
    [SerializeField, Tooltip("추가 점프 키입니다. None으로 끌 수 있습니다. 공중에서 추가 점프는 불가능합니다.")]
    private Key sideViewAlternateJumpKey = Key.None;
    private float sideViewFacing = 1f;
    // 사이드뷰에서는 실제 Transform 위치가 이동/조준 기준입니다.
    public Vector3 GroundPosition => transform.position;
    private float sideViewFallSpeed;
    private bool sideViewJumpCutAvailable;
    private static readonly int SideViewJumpStateHash = Animator.StringToHash("Jump_Right");

    // 일반 이동 애니메이션 경로에서만 호출합니다. 공격/가드/패링/상호작용은 우선권을 유지합니다.
    // 별도 Sprite 필드를 만들지 않아 Animation 창에서 SpriteRenderer.Sprite에 직접 프레임을 넣습니다.
    private bool UpdateSideViewJumpAnimation(Vector2 direction, bool isMoving)
    {
        if (!animator.HasState(0, SideViewJumpStateHash)) return false;
        bool airborne = sideViewFallSpeed < 0f || !IsSideViewGrounded();
        int currentState = animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
        if (!airborne)
        {
            if (currentState == SideViewJumpStateHash)
            {
                animator.speed = baseAnimatorSpeed;
                animator.Play(isMoving ? "Move_Right" : "Player_Idle", 0, 0f);
            }
            return false;
        }

        animator.SetBool(IsMovingHash, isMoving);
        animator.SetInteger(DirectionHash, 2);
        spriteRenderer.flipX = GetSideViewFacing(direction) < 0f;
        animator.speed = baseAnimatorSpeed;
        // 매 프레임 처음부터 재생하지 않습니다. 비반복 클립 끝에서는 마지막 그림을 유지합니다.
        if (currentState != SideViewJumpStateHash) animator.Play(SideViewJumpStateHash, 0, 0f);
        return true;
    }

    private void HandleSideViewJumpInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if ((sideViewJumpKey != Key.None && keyboard[sideViewJumpKey].wasPressedThisFrame)
            || (sideViewAlternateJumpKey != Key.None && keyboard[sideViewAlternateJumpKey].wasPressedThisFrame))
            TryBeginSideViewJump();
    }

    private bool TryBeginSideViewJump()
    {
        if (!isActiveAndEnabled || !guardHasFocus || Time.timeScale <= 0f || IsInstantlyDead
            || isErrorCodexOpen || isAttacking || isGuarding || isPlayingParry || IsGuardRequested()
            || isDashing || didDashThisFrame || IsMovingObject || isSelectingStoredError
            || isReplacingStoredError || environmentPaste.IsBusy || sideViewFallSpeed < 0f
            || sideViewGravity <= 0f || sideViewJumpHeight <= 0f || !IsSideViewGrounded()) return false;

        // 낙하 속도의 음수는 상승입니다. 높이와 중력으로 초기 속도를 계산합니다.
        sideViewFallSpeed = -Mathf.Sqrt(2f * sideViewGravity * sideViewJumpHeight);
        sideViewJumpCutAvailable = sideViewVariableJump;
        return true;
    }

    private void UpdateSideViewJumpRelease()
    {
        if (!sideViewJumpCutAvailable) return;
        if (!sideViewVariableJump || sideViewFallSpeed >= 0f)
        {
            sideViewJumpCutAvailable = false;
            return;
        }
        var keyboard = Keyboard.current;
        bool held = keyboard != null &&
            ((sideViewJumpKey != Key.None && keyboard[sideViewJumpKey].isPressed)
            || (sideViewAlternateJumpKey != Key.None && keyboard[sideViewAlternateJumpKey].isPressed));
        if (held) return;

        // 점프 한 번에 한 번만 감속합니다. 놓고 다시 눌러도 재가속/공중 점프는 없습니다.
        sideViewFallSpeed *= Mathf.Clamp01(sideViewJumpReleaseMultiplier);
        sideViewJumpCutAvailable = false;
    }

    private bool IsSideViewGrounded()
    {
        Physics2D.SyncTransforms();
        var colliders = UnityEngine.Pool.ListPool<Collider2D>.Get();
        try
        {
            GetComponentsInChildren(false, colliders);
            foreach (Collider2D shape in colliders)
            {
                if (!shape.enabled || shape.isTrigger || !shape.gameObject.activeInHierarchy
                    || (shape.attachedRigidbody != null && !shape.attachedRigidbody.simulated)) continue;
                var filter = new ContactFilter2D();
                filter.SetLayerMask(movementBlockingLayers.value & Physics2D.GetLayerCollisionMask(shape.gameObject.layer));
                filter.useTriggers = false;
                // 이동 검사와 같은 월드 경계와 접촉 여유(.02)를 사용합니다.
                Physics2D.BoxCast(shape.bounds.center, shape.bounds.size, 0f,
                    Vector2.down, filter, movementHits, .04f);
                foreach (RaycastHit2D hit in movementHits)
                {
                    if (hit.collider == null || hit.collider.transform.IsChildOf(transform)
                        || Physics2D.GetIgnoreCollision(shape, hit.collider) || hit.normal.y < .5f) continue;
                    // 충돌은 일반 사물도 막지만, 접지/점프 가능한 바닥은 Floor만 인정합니다.
                    if (!IsFloorCollider(hit.collider)) continue;
                    if (hit.distance == 0f && hit.collider.bounds.center.y >= shape.bounds.center.y) continue;
                    return true;
                }
            }
            return false;
        }
        finally { UnityEngine.Pool.ListPool<Collider2D>.Release(colliders); }
    }

    private void UpdateSideViewGravity(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        // 이번 프레임의 상승 이동 전에 키 해제를 반영합니다. 공격/대시 중 해제도 누락하지 않습니다.
        UpdateSideViewJumpRelease();
        // 기존 이동/대시와 같은 Kinematic Cast 경로를 사용해 물리 보정과 직접 이동의 충돌을 피합니다.
        sideViewFallSpeed = Mathf.Min(Mathf.Max(.1f, sideViewMaxFallSpeed), sideViewFallSpeed + Mathf.Max(0f, sideViewGravity) * deltaTime);
        float wanted = sideViewFallSpeed * deltaTime;
        Vector2 moved = MovePlayerWithCollision(Vector2.down * wanted);
        // 바닥뿐 아니라 상승 중 천장에 닿아도 수직 속도를 없앱니다.
        if (Mathf.Abs(moved.y) + .0001f < Mathf.Abs(wanted)) sideViewFallSpeed = 0f;
        if (sideViewFallSpeed >= 0f) sideViewJumpCutAvailable = false;
        if (IsMovingObject && moved.y < -.03f) ReleaseInteraction();
    }

    private float GetSideViewFacing(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > .0001f) sideViewFacing = Mathf.Sign(direction.x);
        return sideViewFacing;
    }




    private void ResetSideViewJumpHeight()
    {
        sideViewJumpCutAvailable = false;
        sideViewFallSpeed = 0f;
    }
}
