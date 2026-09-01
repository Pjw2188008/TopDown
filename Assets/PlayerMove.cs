using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Vector2 lastDirection = Vector2.down; // 기본 방향

    private void Start()
    {
        if (animator == null)
        {
            Debug.LogError("Animator가 할당되지 않았습니다!");
        }
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer가 할당되지 않았습니다!");
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (animator == null || spriteRenderer == null)
        {
            return;
        }

        Vector2 moveDirection = Vector2.zero;

        if (Keyboard.current.wKey.isPressed)
        {
            moveDirection.y += 1f;
        }

        if (Keyboard.current.sKey.isPressed)
        {
            moveDirection.y -= 1f;
        }

        if (Keyboard.current.dKey.isPressed)
        {
            moveDirection.x += 1f;
        }

        if (Keyboard.current.aKey.isPressed)
        {
            moveDirection.x -= 1f;
        }

        if (moveDirection != Vector2.zero)
        {
            moveDirection = moveDirection.normalized;
            lastDirection = moveDirection;  // 마지막 방향 저장
            transform.position += (Vector3)(moveDirection * moveSpeed * Time.deltaTime);
        }

        // 항상 마지막 방향으로 애니메이션 업데이트 (입력 없어도 유지)
        UpdateAnimation(lastDirection);
    }

    private Vector2 GetMouseDirection()
    {
        // 마우스 스크린 좌표를 월드 좌표로 변환
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        // 플레이어에서 마우스로의 벡터
        Vector2 directionToMouse = (mouseWorldPos - transform.position).normalized;
        
        return directionToMouse;
    }

    private void UpdateAnimation(Vector2 direction)
    {
        // WASD 입력 확인
        bool isMovingInput = Keyboard.current.wKey.isPressed || 
                             Keyboard.current.sKey.isPressed ||
                             Keyboard.current.dKey.isPressed ||
                             Keyboard.current.aKey.isPressed;

        if (!isMovingInput)
        {
            animator.SetBool("isMoving", false);
            return;
        }

        animator.SetBool("isMoving", true);

        // 대각선 이동 확인 (X와 Y 모두 입력)
        bool isDiagonal = Keyboard.current.wKey.isPressed && 
                         (Keyboard.current.dKey.isPressed || Keyboard.current.aKey.isPressed) ||
                         Keyboard.current.sKey.isPressed && 
                         (Keyboard.current.dKey.isPressed || Keyboard.current.aKey.isPressed);

        if (isDiagonal)
        {
            // 대각선은 무조건 좌/우 애니메이션
            animator.SetInteger("direction", 2); // Right
            spriteRenderer.flipX = direction.x < 0; // 좌측이면 반전
        }
        else
        {
            // 수직/수평 방향만 처리
            if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
            {
                // 위/아래 이동
                if (direction.y > 0)
                {
                    animator.SetInteger("direction", 0); // Up
                    spriteRenderer.flipX = false;
                }
                else
                {
                    animator.SetInteger("direction", 1); // Down
                    spriteRenderer.flipX = false;
                }
            }
            else
            {
                // 좌/우 이동
                animator.SetInteger("direction", 2); // Right
                spriteRenderer.flipX = direction.x < 0; // 좌측이면 반전
            }
        }
    }
}