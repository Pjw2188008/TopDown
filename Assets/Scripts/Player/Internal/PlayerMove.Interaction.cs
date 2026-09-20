using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>F 물체 잡기/놓기, 공동 이동, 방향별 애니메이션과 안내 UI. PlayerMove partial이므로 따로 부착하지 않습니다.</summary>
public partial class PlayerMove
{
    [Header("상호작용 — F로 물체 옮기기")]
    [Tooltip("가까운 MovableInteractable 물체를 잡거나 놓는 키입니다. 기본 F, 누른 채 유지할 필요는 없습니다.")]
    [SerializeField] private Key interactionKey = Key.F;
    [Tooltip("플레이어 중심에서 물체 Collider의 가장 가까운 지점까지의 상호작용 거리입니다.")]
    [SerializeField, Min(0.1f)] private float interactionRange = 1.5f;
    [Tooltip("Collider 경계 사이의 여유 공간(월드 단위)입니다. 기본 0.3. 잡을 때 물체를 가능한 만큼 벌리고, 이동 입력 시 이 간격으로 물체 반대편에 자리 잡습니다. 0은 최초 간격 보정을 끄며 자리 이동에는 최소 충돌 여유 0.04를 둡니다.")]
    [InspectorName("잡기 간격"), SerializeField, Min(0f)] private float interactionGrabGap = 0.3f;
    [Tooltip("잡을 물체를 검색할 레이어입니다. 기본값은 모든 레이어입니다.")]
    [SerializeField] private LayerMask interactionLayers = ~0;
    [Tooltip("상호작용 접근 및 잡은 상태의 이동을 막는 벽/장애물 레이어입니다. Trigger는 무시합니다.")]
    [SerializeField] private LayerMask interactionBlockingLayers = Physics2D.DefaultRaycastLayers;
    [Tooltip("가까운 물체에 F 안내와 잡은 물체에 놓기 안내를 표시합니다.")]
    [SerializeField] private bool showInteractionPrompt = true;
    [Tooltip("Player 선택 시 상호작용 범위를 노란색 기즈모로 표시합니다.")]
    [SerializeField] private bool showInteractionGizmo = true;
    private MovableInteractable heldInteractable;
    private MovableInteractable nearbyInteractable;
    private Vector2 interactionFacing;
    private int interactionStateHash;
    private GUIStyle interactionPromptStyle;
    private readonly List<RaycastHit2D> interactionHits = new List<RaycastHit2D>(16);
    // Start, destination and four outer corners: a small local route around the held object.
    private readonly Vector2[] interactionRouteNodes = new Vector2[6];
    private readonly float[] interactionRouteCosts = new float[6];
    private readonly int[] interactionRoutePrevious = new int[6];
    private readonly bool[] interactionRouteVisited = new bool[6];
    private bool IsMovingObject => heldInteractable != null && heldInteractable.isActiveAndEnabled && heldInteractable.IsHeldBy(transform);

    private bool CanBeginInteraction => guardHasFocus && !isEditMode && !isErrorCodexOpen && !isAttacking
        && !isGuarding && !isPlayingParry && !isDashing && !isReplacingStoredError && !environmentPaste.IsBusy
        && Time.timeScale > 0f && animator != null && spriteRenderer != null
        && (Mouse.current == null || !Mouse.current.rightButton.isPressed);

    private bool HandleInteractionInput()
    {
        if (heldInteractable != null && !IsMovingObject) ReleaseInteraction();
        else if (heldInteractable == null && interactionStateHash != 0) ReleaseInteraction();
        nearbyInteractable = IsMovingObject || !CanBeginInteraction ? null : FindNearbyInteractable();
        if (Keyboard.current == null || !Keyboard.current[interactionKey].wasPressedThisFrame) return false;
        if (IsMovingObject) { ReleaseInteraction(); return true; }
        if (!CanBeginInteraction || nearbyInteractable == null) return false;
        if (!nearbyInteractable.TryGrab(transform)) return false;
        heldInteractable = nearbyInteractable; nearbyInteractable = null;
        EnsureInteractionGrabGap();
        interactionFacing = GetCardinalAttackDirection((Vector2)(heldInteractable.transform.position - transform.position));
        UpdateInteractionAnimation(false);
        return true;
    }



    private MovableInteractable FindNearbyInteractable()
    {
        Physics2D.SyncTransforms();
        MovableInteractable best = null;
        float distance = float.PositiveInfinity;
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(transform.position, Mathf.Max(.1f, interactionRange), interactionLayers))
        {
            if (collider.isTrigger || collider.transform.IsChildOf(transform)) continue;
            MovableInteractable candidate = collider.GetComponentInParent<MovableInteractable>();
            if (candidate == null || !candidate.IsAvailable) continue;
            Vector2 nearest = collider.ClosestPoint(transform.position);
            float candidateDistance = (nearest - (Vector2)transform.position).sqrMagnitude;
            if (candidateDistance >= distance || !HasInteractionLineOfSight(candidate, nearest)) continue;
            distance = candidateDistance; best = candidate;
        }
        return best;
    }

    private bool HasInteractionLineOfSight(MovableInteractable candidate, Vector2 end)
    {
        foreach (RaycastHit2D hit in Physics2D.LinecastAll(transform.position, end, interactionBlockingLayers))
            if (hit.collider != null && !hit.collider.isTrigger && !hit.collider.transform.IsChildOf(transform)
                && !hit.collider.transform.IsChildOf(candidate.transform)) return false;
        return true;
    }

    private bool MoveWithInteractable(Vector2 movement)
    {
        if (!IsMovingObject || movement.sqrMagnitude <= .0001f) return false;
        // 일반 이동처럼 입력 방향을 바라봅니다. 대각선은 수평 방향을 우선합니다.
        // 입력이 없을 때는 마지막 방향을 유지하여 해당 방향의 정지 클립을 재생합니다.
        interactionFacing = movement.x != 0f
            ? new Vector2(Mathf.Sign(movement.x), 0f)
            : new Vector2(0f, Mathf.Sign(movement.y));
        lastDirection = interactionFacing;
        // 먼저 입력 방향의 반대편에 섭니다. 자리 이동 중에는 물체를 움직이지 않습니다.
        if (!TryRepositionForInteraction(Time.deltaTime, out bool repositioned)) return repositioned;
        Vector2 direction = movement.normalized;
        float wanted = Mathf.Max(0f, moveSpeed) * heldInteractable.MoveSpeedMultiplier * Time.deltaTime;
        Physics2D.SyncTransforms();
        float allowed = InteractionMotion.AllowedDistance(transform, heldInteractable.transform, direction, wanted, interactionBlockingLayers, interactionHits);
        allowed = Mathf.Min(allowed, InteractionMotion.AllowedDistance(heldInteractable.transform, transform, direction, wanted, interactionBlockingLayers, interactionHits));
        Vector2 displacement = direction * allowed;
        ApplyPlayerDisplacement(displacement);
        heldInteractable.MoveBy(transform, displacement);
        Physics2D.SyncTransforms();
        lastDirection = interactionFacing;
        return allowed > .00001f;
    }


    private void UpdateInteractionAnimation(bool moving)
    {
        if (animator == null || spriteRenderer == null) return;
        string suffix = interactionFacing.y > .5f ? "Up" : interactionFacing.y < -.5f ? "Down" : "Right";
        int hash = Animator.StringToHash((moving ? "Interact_Move_" : "Interact_Idle_") + suffix);
        animator.SetBool(IsMovingHash, false); // Suppress ordinary movement transitions while holding.
        spriteRenderer.flipX = suffix == "Right" && interactionFacing.x < 0f;
        if (interactionStateHash == hash) return;
        interactionStateHash = hash;
        if (animator.HasState(0, hash)) { animator.speed = baseAnimatorSpeed; animator.Play(hash, 0, 0f); }
    }

    private void ReleaseInteraction()
    {
        if (heldInteractable != null) heldInteractable.Release(transform);
        heldInteractable = null; nearbyInteractable = null;
        if (interactionStateHash != 0 && animator != null)
        { animator.speed = baseAnimatorSpeed; animator.SetBool(IsMovingHash, false); animator.Play("Player_Idle", 0, 0f); }
        interactionStateHash = 0;
    }

    private void DrawInteractionPrompt()
    {
        if (!showInteractionPrompt || isErrorCodexOpen || Camera.main == null) return;
        MovableInteractable target = IsMovingObject ? heldInteractable : nearbyInteractable;
        if (target == null || !target.isActiveAndEnabled) return;
        Vector3 screen = Camera.main.WorldToScreenPoint(target.transform.position);
        if (screen.z <= 0 || screen.x < 0 || screen.x > Screen.width || screen.y < 0 || screen.y > Screen.height) return;
        if (interactionPromptStyle == null) interactionPromptStyle = new GUIStyle(GUI.skin.box)
        { alignment = TextAnchor.MiddleCenter, fontSize = 14, wordWrap = true };
        string message = IsMovingObject ? $"[{interactionKey}] 놓기 · WASD 옮기기" : $"[{interactionKey}] {target.InteractionName} 잡기";
        GUI.Box(new Rect(screen.x - 120, Screen.height - screen.y + 38, 240, 44), message, interactionPromptStyle);
    }

    private void DrawInteractionGizmo()
    {
        if (!showInteractionGizmo) return;
        Color previous = Gizmos.color; Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(.1f, interactionRange)); Gizmos.color = previous;
    }
}
