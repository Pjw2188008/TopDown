using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>사이드뷰 전용 중력, 접지 검사와 단발 점프입니다. PlayerMove partial이므로 따로 붙이지 않습니다.</summary>
public partial class PlayerMove
{
    [SerializeField, Tooltip("켜면 좌우 이동/공격/가드/패링/대시와 중력을 사용합니다. 끄면 기존 탑다운 방식입니다. 모드별 Animator도 연결하세요. Play 전에 설정합니다.")]
    private bool sideViewMode;
    [SerializeField, Tooltip("XY 바닥 8방향 이동입니다. 점프는 본체를 올리되 Collider 오프셋으로 바닥 판정을 유지합니다. 별도 Visual 자식은 필요 없습니다. Play 전에 설정하세요.")]
    private bool sideViewDepthMovement;
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
    public bool IsSideView => sideViewMode;
    public bool IsSideViewDepthMovement => IsSideView && sideViewDepthMovement;
    /// <summary>2.5D 기본 바닥에서의 높이입니다. 발판에 서 있으면 해당 발판 높이를 포함합니다.</summary>
    public float SideViewHeight => IsSideViewDepthMovement ? sideViewHeight : 0f;
    private float sideViewHeight;
    private float sideViewFacing = 1f;
    // 렌더링 위치에서 이미 적용한 높이를 빼면 바닥 좌표입니다.
    public Vector3 GroundPosition => transform.position - Vector3.up * appliedSideViewHeight;
    private float appliedSideViewHeight;
    private Collider2D[] sideViewGroundColliders;
    private Vector2[] sideViewGroundOffsets;
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
        if (!IsSideView || keyboard == null) return;
        if ((sideViewJumpKey != Key.None && keyboard[sideViewJumpKey].wasPressedThisFrame)
            || (sideViewAlternateJumpKey != Key.None && keyboard[sideViewAlternateJumpKey].wasPressedThisFrame))
            TryBeginSideViewJump();
    }

    private bool TryBeginSideViewJump()
    {
        if (!IsSideView || !isActiveAndEnabled || !guardHasFocus || Time.timeScale <= 0f || IsInstantlyDead
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
        if (IsSideViewDepthMovement)
            return sideViewFallSpeed >= 0f && Mathf.Abs(sideViewHeight - GetSideViewSupportHeight()) <= .0001f;
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
                    if (IsSideView && !IsFloorCollider(hit.collider)) continue;
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
        if (!IsSideView || deltaTime <= 0f) return;
        // 이번 프레임의 상승 이동 전에 키 해제를 반영합니다. 공격/대시 중 해제도 누락하지 않습니다.
        UpdateSideViewJumpRelease();
        if (IsSideViewDepthMovement)
        {
            float support = GetSideViewSupportHeight();
            if (sideViewFallSpeed >= 0f && Mathf.Abs(sideViewHeight - support) <= .0001f)
            { sideViewHeight = support; sideViewFallSpeed = 0f; UpdateSideViewVisualHeight(); return; }
            sideViewFallSpeed = Mathf.Min(Mathf.Max(.1f, sideViewMaxFallSpeed), sideViewFallSpeed + Mathf.Max(0f, sideViewGravity) * deltaTime);
            float nextHeight = sideViewHeight - sideViewFallSpeed * deltaTime;
            // 이전 높이와 하강할 높이를 비교하여 저프레임에서도 윗면을 뚫지 않습니다.
            if (sideViewFallSpeed >= 0f && nextHeight <= support)
            { sideViewHeight = support; sideViewFallSpeed = 0f; }
            else sideViewHeight = Mathf.Max(0f, nextHeight);
            if (sideViewFallSpeed >= 0f) sideViewJumpCutAvailable = false;
            UpdateSideViewVisualHeight();
            return;
        }
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

    private float GetSideViewSupportHeight()
    {
        // Collider는 점프 중에도 바닥에 남으므로 발 위치로 사용할 수 있습니다.
        Vector2 feet = GroundPosition;
        var body = GetComponent<Collider2D>();
        if (body != null && body.enabled) feet = new Vector2(body.bounds.center.x, body.bounds.min.y);
        return SideViewJumpPlatform.FindSupportHeight(feet, sideViewHeight);
    }

    private void UpdateSideViewVisualHeight()
    {
        if (!IsSideViewDepthMovement) return;
        ApplySideViewHeight(sideViewHeight);
    }

    private void ApplySideViewHeight(float height)
    {
        if (sideViewGroundColliders == null)
        {
            sideViewGroundColliders = GetComponents<Collider2D>();
            sideViewGroundOffsets = new Vector2[sideViewGroundColliders.Length];
            for (int i = 0; i < sideViewGroundColliders.Length; i++) sideViewGroundOffsets[i] = sideViewGroundColliders[i].offset;
        }
        float delta = height - appliedSideViewHeight;
        if (Mathf.Abs(delta) > 0f) ApplyPlayerDisplacement(Vector2.up * delta);
        appliedSideViewHeight = height;
        // 크기가 바뀌어도 월드 높이에 맞춰 Collider의 로컬 오프셋을 보정합니다.
        Vector2 compensation = transform.InverseTransformVector(Vector3.up * height);
        for (int i = 0; i < sideViewGroundColliders.Length; i++)
            if (sideViewGroundColliders[i] != null) sideViewGroundColliders[i].offset = sideViewGroundOffsets[i] - compensation;
        Physics2D.SyncTransforms();
    }

    private void ResetSideViewJumpHeight()
    {
        sideViewJumpCutAvailable = false;
        sideViewHeight = 0f;
        sideViewFallSpeed = 0f;
        if (appliedSideViewHeight != 0f || sideViewGroundColliders != null) ApplySideViewHeight(0f);
    }
}
