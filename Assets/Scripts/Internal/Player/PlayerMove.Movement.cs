using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>PlayerMove의 이동·커서·이동 애니메이션 구현 부분입니다. 별도 컴포넌트가 아니므로 직접 부착하지 않습니다.</summary>
public partial class PlayerMove
{
    private Vector2 GetMovementInput()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed)
        {
            input.y += 1f;
        }

        if (Keyboard.current.sKey.isPressed)
        {
            input.y -= 1f;
        }

        if (Keyboard.current.dKey.isPressed)
        {
            input.x += 1f;
        }

        if (Keyboard.current.aKey.isPressed)
        {
            input.x -= 1f;
        }

        return input;
    }

    private Vector2 GetMouseDirection()
    {
        if (Mouse.current == null || Camera.main == null)
            return lastDirection.sqrMagnitude > 0f ? lastDirection.normalized : Vector2.right;

        // 마우스 스크린 좌표를 월드 좌표로 변환
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        // 플레이어에서 마우스로의 벡터
        Vector2 directionToMouse = (Vector2)(mouseWorldPos - transform.position);

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
        // 스크립트는 이동 상태만 전달하고, 실제 클립 전환은 Controller가 담당합니다.
        animator.SetBool(IsMovingHash, isMoving);
        if (!isMoving)
            return;

        // 대각선은 수평 방향을 우선하여 좌우 이동 클립을 사용합니다.
        int movementDirection = direction.x != 0f ? 2 : (direction.y > 0f ? 0 : 1);
        animator.SetInteger(DirectionHash, movementDirection);

        // 왼쪽과 왼쪽 대각선만 오른쪽 클립을 뒤집습니다. 위/아래는 뒤집지 않습니다.
        spriteRenderer.flipX = movementDirection == 2 && direction.x < 0f;
    }

    private void EnsureProjectileCollider()
    {
        if (GetComponent<Collider2D>() != null)
        {
            return;
        }

        BoxCollider2D playerCollider = gameObject.AddComponent<BoxCollider2D>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            playerCollider.size = spriteRenderer.sprite.bounds.size;
        }
    }
}
