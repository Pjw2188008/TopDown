using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 0.35f;
    [SerializeField] private float attackRangeSide = 0.75f;
    [SerializeField] private Vector2 attackSizeSide = new Vector2(0.8f, 0.8f);
    [SerializeField] private LayerMask enemyLayer;

    private Vector2 lastDirection = Vector2.right;
    private int currentDirection = -1;
    private bool currentMovingState = false;
    private float nextAttackTime;
    private bool isAttacking;
    private float attackTimer;

    private void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (animator == null)
        {
            Debug.LogError("Animator가 할당되지 않았습니다! Player 오브젝트에 Animator를 추가하거나 인스펙터에서 연결해주세요.");
        }

        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer가 할당되지 않았습니다! Player 오브젝트에 SpriteRenderer를 추가하거나 인스펙터에서 연결해주세요.");
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

        Vector2 moveDirection = GetMovementInput();
        bool isMoving = moveDirection != Vector2.zero;

        if (isMoving)
        {
            moveDirection = moveDirection.normalized;
            lastDirection = moveDirection;
            transform.position += (Vector3)(moveDirection * moveSpeed * Time.deltaTime);
        }

        UpdateAnimation(isMoving ? moveDirection : lastDirection, isMoving);

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryAttack();
        }

        if (isAttacking)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                isAttacking = false;
                attackTimer = 0f;
                animator.SetBool("isMoving", false);
                animator.Play("Player_Idle", 0, 0f);
                animator.ResetTrigger("Attack");
            }
        }
    }

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

    private void TryAttack()
    {
        if (Time.time < nextAttackTime || isAttacking)
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        isAttacking = true;
        attackTimer = 0.25f;

        Vector2 facing = GetMouseDirection();
        bool facingLeft = facing.x < 0f;

        animator.SetInteger("direction", 2);
        animator.SetBool("isMoving", false);

        animator.Play("Attack_Right", 0, 0f);
        spriteRenderer.flipX = facingLeft;

        AttackHit(facingLeft);
    }

    public void AttackFinished()
    {
        isAttacking = false;
        attackTimer = 0f;
        animator.SetBool("isMoving", false);
        animator.Play("Player_Idle", 0, 0f);
    }

    private void AttackHit(bool facingLeft)
    {
        Vector2 center = transform.position;
        Vector2 offset = Vector2.right * (facingLeft ? -1f : 1f) * attackRangeSide;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center + offset, attackSizeSide, 0f, enemyLayer);

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == gameObject)
            {
                continue;
            }

            Debug.Log("근접 공격 히트: " + hit.name);
            // hit.GetComponent<Enemy>().TakeDamage(1);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Vector2 center = transform.position;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireCube(center + Vector2.right * attackRangeSide, attackSizeSide);
        Gizmos.DrawWireCube(center + Vector2.left * attackRangeSide, attackSizeSide);
    }

    private Vector2 GetMouseDirection()
    {
        // 마우스 스크린 좌표를 월드 좌표로 변환
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        // 플레이어에서 마우스로의 벡터
        Vector2 directionToMouse = (mouseWorldPos - transform.position).normalized;

        return directionToMouse;
    }

    private void UpdateAnimation(Vector2 direction, bool isMoving)
    {
        if (animator == null || spriteRenderer == null)
        {
            return;
        }

        if (!isMoving)
        {
            if (currentMovingState)
            {
                animator.SetBool("isMoving", false);
                animator.Play("Player_Idle", 0, 0f);
                currentMovingState = false;
                currentDirection = -1;
            }
            return;
        }

        if (!currentMovingState)
        {
            animator.SetBool("isMoving", true);
            animator.Play("Move_Right", 0, 0f);
            animator.SetInteger("direction", 2);
            currentMovingState = true;
            currentDirection = 2;
        }

        spriteRenderer.flipX = direction.x < 0f;
    }
}