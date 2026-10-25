using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>PlayerMove의 이동·커서·이동 애니메이션 구현 부분입니다. 별도 컴포넌트가 아니므로 직접 부착하지 않습니다.</summary>
public partial class PlayerMove
{
    /// <summary>걷기/달리기를 나누지 않고 기존 달리기 속도로 항상 이동합니다. 이동 클립은 그대로 사용합니다.</summary>
    private float GetCurrentMoveSpeed()
    {
        return Mathf.Max(0f, moveSpeed) * Mathf.Max(1f, runSpeedMultiplier);
    }

    private Vector2 GetMovementInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return Vector2.zero;
        bool left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
        bool right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
        return Vector2.right * ((right ? 1f : 0f) - (left ? 1f : 0f));
    }

    private Vector2 GetMouseDirection()
    {
        // 일반 전투는 마지막 이동 방향, 편집 모드는 마우스 조준을 유지합니다.
        if (!isEditMode) return Vector2.right * GetSideViewFacing(lastDirection);
        if (Mouse.current == null || Camera.main == null)
            return lastDirection.sqrMagnitude > 0f ? lastDirection.normalized : Vector2.right;

        // 마우스 스크린 좌표를 월드 좌표로 변환
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        // 플레이어에서 마우스로의 벡터
        Vector2 directionToMouse = (Vector2)(mouseWorldPos - GroundPosition);

        return directionToMouse.sqrMagnitude > 0.0001f ? directionToMouse.normalized
            : (lastDirection.sqrMagnitude > 0f ? lastDirection.normalized : Vector2.right);
    }

    private void UpdateAnimation(Vector2 direction, bool isMoving)
    {
        if (animator == null || spriteRenderer == null)
        {
            return;
        }

        // 클립/프레임은 Animator와 Animation 창에서 관리합니다.
        if (UpdateSideViewJumpAnimation(direction, isMoving)) return;
        // 스크립트는 이동 상태만 전달하고, 실제 클립 전환은 Controller가 담당합니다.
        animator.SetBool(IsMovingHash, isMoving);
        if (!isMoving)
            return;

        // 오른쪽 이동 클립을 좌우 반전하여 사용합니다.
        const int movementDirection = 2;
        animator.SetInteger(DirectionHash, movementDirection);

        spriteRenderer.flipX = GetSideViewFacing(direction) < 0f;
    }

    private void EnsureProjectileCollider()
    {
        Collider2D existing = GetComponent<Collider2D>();
        if (existing != null)
        {
            existing.isTrigger = false;
            existing.enabled = true;
            return;
        }

        BoxCollider2D playerCollider = gameObject.AddComponent<BoxCollider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            playerCollider.size = spriteRenderer.sprite.bounds.size;
        }
    }
}
