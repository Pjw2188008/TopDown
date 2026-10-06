using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 사이드뷰 비교 테스트 전용 플레이어입니다. 루트에 부착합니다.
/// A/D 또는 좌우 방향키로 X축만 조작하며, Y축은 중력/바닥 충돌에 맡깁니다.
/// 기존 PlayerMove/오류/저장은 사용하지 않습니다. 피해 전달 규약만 공용으로 사용합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(SpriteRenderer))]
public sealed partial class SideViewPlayer : MonoBehaviour
{
    [SerializeField, Min(0f), Tooltip("좌우 이동 속도(월드 단위/초)입니다. 점프나 상하 이동 입력은 사용하지 않습니다.")]
    private float moveSpeed = 5f;
    [SerializeField, Tooltip("원본 그림이 오른쪽을 향하면 켭니다. 왼쪽 이동 시 SpriteRenderer만 뒤집습니다.")]
    private bool spriteFacesRight = true;

    private Rigidbody2D body;
    private SpriteRenderer visual;
    private Animator animator;
    private float horizontal;
    private bool moving;
    private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
    private static readonly int WalkState = Animator.StringToHash("Base Layer.Walk");

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        visual = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        horizontal = 0f;
        if (isAttacking) return;
        if (Application.isFocused && Time.timeScale > 0f && keyboard != null)
            horizontal = ReadHorizontalInput(keyboard);
        if (horizontal != 0f)
        {
            facing = horizontal < 0f ? -1 : 1;
            visual.flipX = (facing < 0) == spriteFacesRight;
        }
        if (Application.isFocused && Time.timeScale > 0f && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryAttack();
            if (isAttacking) return;
        }
        SetAnimation(horizontal != 0f);
    }

    private static float ReadHorizontalInput(Keyboard keyboard)
    {
        if (keyboard == null) return 0f;
        bool left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
        bool right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
        return (right ? 1f : 0f) - (left ? 1f : 0f);
    }

    private void FixedUpdate()
    {
        // Transform 순간이동 대신 물리 속도를 사용해 바닥/벽 충돌을 유지합니다.
        body.linearVelocity = new Vector2(isAttacking ? 0f : horizontal * moveSpeed, body.linearVelocity.y);
    }

    private void SetAnimation(bool nextMoving)
    {
        if (moving == nextMoving) return;
        moving = nextMoving;
        if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) return;
        int state = moving ? WalkState : IdleState;
        if (animator.HasState(0, state)) animator.Play(state, 0, 0f);
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) StopHorizontalMotion();
    }

    private void OnDisable()
    {
        CancelAttack();
        StopHorizontalMotion();
    }

    private void StopHorizontalMotion()
    {
        horizontal = 0f;
        if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        SetAnimation(false);
    }
}
