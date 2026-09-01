using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerMove : MonoBehaviour
{
    [Tooltip("플레이어 이동 속도입니다. 기본 이동 속도를 조절합니다.")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("플레이어 애니메이터입니다. 이동/공격 애니메이션을 제어합니다.")]
    [SerializeField] private Animator animator;

    [Tooltip("플레이어의 SpriteRenderer입니다. 방향 전환과 시각 효과에 사용됩니다.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Edit Mode")]
    [Tooltip("E 키로 편집 모드를 켜고 끕니다. 편집 모드에서는 환경 오류를 저장할 수 있습니다.")]
    [SerializeField] private Key editModeKey = Key.E;

    [Tooltip("편집 모드가 켜졌을 때, 시간 흐름이 몇 배 느려지는지 설정합니다.")]
    [SerializeField] private float editTimeScale = 0.35f;

    [Tooltip("편집 모드 화면이 얼마나 어두워지는지 설정합니다. 0~1 범위로 조절됩니다.")]
    [SerializeField] private float editModeDarkness = 0.45f;

    [Tooltip("어두워지는 효과가 얼마나 부드럽게 전환되는지 설정합니다.")]
    [SerializeField] private float editModeFadeSpeed = 5f;

    [Tooltip("환경용 거대화 오류를 저장한 뒤, 최대 몇 번 붙여넣기할 수 있는지 설정합니다.")]
    [SerializeField] private int maxEnvironmentErrorUses = 3;

    [Header("Player Giant Error")]
    [Tooltip("일반 상태에서 Q 키를 눌렀을 때, 공격 범위가 몇 배로 늘어나는지 설정합니다.")]
    [SerializeField] private float playerErrorEffectMultiplier = 2.5f;

    [Tooltip("플레이어용 거대화 오류 버프가 유지되는 시간입니다.")]
    [SerializeField] private float playerErrorDuration = 6f;

    [Tooltip("플레이어용 거대화 오류를 몇 번 사용할 수 있는지 설정합니다. 보통 1회로 충분합니다.")]
    [SerializeField] private int maxPlayerErrorUses = 1;

    [Header("Attack")]
    [Tooltip("공격 후 다음 공격까지의 쿨타임입니다.")]
    [SerializeField] private float attackCooldown = 0.35f;

    [Tooltip("기본 공격 범위의 거리입니다. 플레이어 앞쪽으로 얼마나 넓게 공격하는지 설정합니다.")]
    [SerializeField] private float attackRangeSide = 0.75f;

    [Tooltip("공격 판정의 크기입니다. 사각형 판정의 가로/세로 크기를 조절합니다.")]
    [SerializeField] private Vector2 attackSizeSide = new Vector2(0.8f, 0.8f);

    [Tooltip("공격이 감지될 적 레이어입니다. 적 오브젝트가 이 레이어에 있어야 공격 판정에 걸립니다.")]
    [SerializeField] private LayerMask enemyLayer;

    private Vector2 lastDirection = Vector2.right;
    private int currentDirection = -1;
    private bool currentMovingState = false;
    private float nextAttackTime;
    private bool isAttacking;
    private float attackTimer;
    private bool isEditMode;
    private Image editModeOverlay;

    private float storedEnvironmentErrorMultiplier = 0f;
    private bool hasStoredEnvironmentError = false;
    private int environmentErrorUsesLeft = 0;

    private float storedPlayerErrorMultiplier = 0f;
    private bool hasStoredPlayerError = false;
    private int playerErrorUsesLeft = 0;

    private float playerErrorTimer = 0f;

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

        CreateEditModeOverlay();
        UpdateEditModeVisual();
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            ToggleEditMode();
        }

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            if (isEditMode)
            {
                TryPasteError();
                return;
            }

            TryActivatePlayerErrorBuff();
            return;
        }

        if (animator == null || spriteRenderer == null)
        {
            UpdateEditModeVisual();
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

        if (playerErrorTimer > 0f)
        {
            playerErrorTimer -= Time.deltaTime;
            if (playerErrorTimer <= 0f)
            {
                playerErrorTimer = 0f;
                Debug.Log("거대화 오류 버프 종료");
            }
        }

        UpdateEditModeVisual();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    private void ToggleEditMode()
    {
        isEditMode = !isEditMode;
        Time.timeScale = isEditMode ? editTimeScale : 1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        if (isEditMode)
        {
            Debug.Log("편집 모드 활성화 - 시간 흐름이 느려집니다.");
        }
        else
        {
            Debug.Log("편집 모드 종료");
        }
    }

    private void CreateEditModeOverlay()
    {
        GameObject canvasObject = new GameObject("EditModeOverlayCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject overlayObject = new GameObject("EditModeOverlay");
        overlayObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rectTransform = overlayObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        editModeOverlay = overlayObject.AddComponent<Image>();
        editModeOverlay.color = new Color(0f, 0f, 0f, 0f);
        editModeOverlay.raycastTarget = false;
    }

    private void UpdateEditModeVisual()
    {
        if (editModeOverlay == null)
        {
            return;
        }

        Color targetColor = isEditMode ? new Color(0f, 0f, 0f, editModeDarkness) : new Color(0f, 0f, 0f, 0f);
        editModeOverlay.color = Color.Lerp(editModeOverlay.color, targetColor, Time.unscaledDeltaTime * editModeFadeSpeed);
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
        Vector2 offset = Vector2.right * (facingLeft ? -1f : 1f) * GetCurrentAttackRange();

        Collider2D[] hits = Physics2D.OverlapBoxAll(center + offset, attackSizeSide * GetCurrentAttackScale(), 0f, enemyLayer);

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == gameObject)
            {
                continue;
            }

            if (hit.TryGetComponent<GiantErrorEffect>(out var giantError))
            {
                if (!isEditMode)
                {
                    Debug.Log("일반 상태에서는 거대화 오류를 저장할 수 없습니다.");
                    continue;
                }

                if (hasStoredEnvironmentError)
                {
                    Debug.Log("이미 저장된 환경용 거대화 오류가 있어 추가로 얻을 수 없습니다.");
                    continue;
                }

                storedEnvironmentErrorMultiplier = giantError.CurrentMultiplier;
                environmentErrorUsesLeft = Mathf.Max(1, maxEnvironmentErrorUses);
                hasStoredEnvironmentError = true;
                giantError.ResetScale();
                Debug.Log("환경용 거대화 오류가 저장되었습니다. 남은 사용 횟수: " + environmentErrorUsesLeft);
                continue;
            }

            Debug.Log("근접 공격 히트: " + hit.name);
            // hit.GetComponent<Enemy>().TakeDamage(1);
        }
    }

    private void TryPasteError()
    {
        if (!isEditMode || !hasStoredEnvironmentError || environmentErrorUsesLeft <= 0)
        {
            if (hasStoredEnvironmentError || environmentErrorUsesLeft <= 0)
            {
                ClearEnvironmentError();
            }

            Debug.Log("붙여넣을 저장된 환경용 거대화 오류가 없습니다.");
            return;
        }

        if (Mouse.current == null || Camera.main == null)
        {
            return;
        }

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseWorldPos.z = 0f;

        Collider2D targetCollider = Physics2D.OverlapPoint(mouseWorldPos);
        if (targetCollider == null)
        {
            GameObject clickedObject = GetSpriteObjectAtMousePosition(mouseWorldPos);
            if (clickedObject == null)
            {
                Debug.Log("커서 위치에 붙여넣기 대상이 없습니다.");
                return;
            }

            targetCollider = clickedObject.GetComponent<Collider2D>();
            if (targetCollider == null)
            {
                BoxCollider2D collider = clickedObject.AddComponent<BoxCollider2D>();
                SpriteRenderer spriteRenderer = clickedObject.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    collider.size = spriteRenderer.bounds.size;
                }
                targetCollider = collider;
            }
        }

        GameObject targetObject = targetCollider.gameObject;
        GiantErrorEffect effect = targetObject.GetComponent<GiantErrorEffect>();

        if (effect == null)
        {
            effect = targetObject.AddComponent<GiantErrorEffect>();
        }

        effect.Trigger(storedEnvironmentErrorMultiplier);
        environmentErrorUsesLeft--;
        Debug.Log("환경용 거대화 오류가 붙여넣기 되었습니다. 배수: " + storedEnvironmentErrorMultiplier + "x | 남은 사용: " + environmentErrorUsesLeft);

        if (environmentErrorUsesLeft <= 0)
        {
            ClearEnvironmentError();
            Debug.Log("환경용 거대화 오류가 모두 소모되어 더 이상 다시 얻을 수 없습니다.");
        }
    }

    private void TryActivatePlayerErrorBuff()
    {
        if (isEditMode)
        {
            Debug.Log("편집 모드에서는 오류를 저장하지 않고, 일반 상태에서만 Q로 버프를 사용할 수 있습니다.");
            return;
        }

        if (playerErrorTimer > 0f)
        {
            Debug.Log("이미 거대화 오류 버프가 활성화 중입니다.");
            return;
        }

        storedPlayerErrorMultiplier = playerErrorEffectMultiplier;
        playerErrorUsesLeft = Mathf.Max(1, maxPlayerErrorUses);
        playerErrorTimer = playerErrorDuration;
        Debug.Log("플레이어 거대화 오류 활성화! 공격 범위 " + storedPlayerErrorMultiplier + "배 증가, 지속 시간 " + playerErrorDuration + "초");
    }

    private float GetCurrentAttackRange()
    {
        return attackRangeSide * GetCurrentAttackScale();
    }

    private float GetCurrentAttackScale()
    {
        if (playerErrorTimer > 0f && storedPlayerErrorMultiplier > 0f)
        {
            return Mathf.Max(1f, storedPlayerErrorMultiplier);
        }

        return 1f;
    }

    private void ClearEnvironmentError()
    {
        storedEnvironmentErrorMultiplier = 0f;
        environmentErrorUsesLeft = 0;
        hasStoredEnvironmentError = false;
    }

    private void ClearPlayerError()
    {
        storedPlayerErrorMultiplier = 0f;
        playerErrorUsesLeft = 0;
        hasStoredPlayerError = false;
        playerErrorTimer = 0f;
    }

    private GameObject GetSpriteObjectAtMousePosition(Vector3 mouseWorldPos)
    {
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || renderer.bounds.Contains(mouseWorldPos) == false)
            {
                continue;
            }

            return renderer.gameObject;
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Vector2 center = transform.position;
        float currentAttackRange = GetCurrentAttackRange();
        Vector2 currentAttackSize = attackSizeSide * GetCurrentAttackScale();

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireCube(center + Vector2.right * currentAttackRange, currentAttackSize);
        Gizmos.DrawWireCube(center + Vector2.left * currentAttackRange, currentAttackSize);

        if (playerErrorTimer > 0f)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.8f, 0.7f);
            Gizmos.DrawWireCube(center + Vector2.right * currentAttackRange, currentAttackSize);
            Gizmos.DrawWireCube(center + Vector2.left * currentAttackRange, currentAttackSize);
        }
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