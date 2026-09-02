using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerMove : MonoBehaviour
{
    private enum StoredErrorType
    {
        None,
        Giant,
        Acceleration
    }

    private const int MaxStoredErrors = 2;
    private static readonly int AttackRightStateHash = Animator.StringToHash("Attack_Right");

    [Header("플레이어 기본 설정")]
    [Tooltip("플레이어 이동 속도입니다. 기본 이동 속도를 조절합니다.")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("플레이어 애니메이터입니다. 이동/공격 애니메이션을 제어합니다.")]
    [SerializeField] private Animator animator;

    [Tooltip("플레이어의 SpriteRenderer입니다. 방향 전환과 시각 효과에 사용됩니다.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("편집 모드")]
    [Tooltip("E 키로 편집 모드를 켜고 끕니다. 편집 모드에서는 환경 오류를 저장할 수 있습니다.")]
    [SerializeField] private Key editModeKey = Key.E;

    [Tooltip("편집 모드가 켜졌을 때, 시간 흐름이 몇 배 느려지는지 설정합니다.")]
    [SerializeField] private float editTimeScale = 0.35f;

    [Tooltip("편집 모드 화면이 얼마나 어두워지는지 설정합니다. 0~1 범위로 조절됩니다.")]
    [SerializeField] private float editModeDarkness = 0.45f;

    [Tooltip("어두워지는 효과가 얼마나 부드럽게 전환되는지 설정합니다.")]
    [SerializeField] private float editModeFadeSpeed = 5f;

    [Tooltip("거대화 오류를 Cut한 뒤 전투 기술에 사용할 수 있는 횟수입니다. 환경 Paste는 1회 사용 즉시 오류가 보관함에서 제거됩니다.")]
    [SerializeField] private int maxEnvironmentErrorUses = 3;

    [Header("전투 기술 - 거대화 오류")]
    [Tooltip("일반 상태에서 Q 키를 눌렀을 때, 공격 범위가 몇 배로 늘어나는지 설정합니다.")]
    [SerializeField] private float playerErrorEffectMultiplier = 2.5f;

    [Tooltip("플레이어용 거대화 오류 버프가 유지되는 시간입니다.")]
    [SerializeField] private float playerErrorDuration = 6f;

    [Tooltip("플레이어용 거대화 오류를 몇 번 사용할 수 있는지 설정합니다. 보통 1회로 충분합니다.")]
    [SerializeField] private int maxPlayerErrorUses = 1;

    [Header("전투 기술 - 가속 오류")]
    [Tooltip("가속 오류를 전투 기술에 Paste했을 때 효과가 유지되는 시간입니다.")]
    [SerializeField, Min(0.1f)] private float accelerationAttackDuration = 5f;

    [Header("기본 공격")]
    [Tooltip("공격 후 다음 공격까지의 쿨타임입니다.")]
    [SerializeField] private float attackCooldown = 0.35f;

    [Tooltip("기본 공격 범위의 거리입니다. 플레이어 앞쪽으로 얼마나 넓게 공격하는지 설정합니다.")]
    [SerializeField] private float attackRangeSide = 0.75f;

    [Tooltip("공격 판정의 크기입니다. 사각형 판정의 가로/세로 크기를 조절합니다.")]
    [SerializeField] private Vector2 attackSizeSide = new Vector2(0.8f, 0.8f);

    [Tooltip("공격이 감지될 적 레이어입니다. 적 오브젝트가 이 레이어에 있어야 공격 판정에 걸립니다.")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("공격 이펙트")]
    [Tooltip("공격 마지막 프레임에 공격 범위 위로 표시할 스프라이트입니다.")]
    [SerializeField] private Sprite attackEffectSprite;

    [Tooltip("공격 애니메이션에서 이펙트와 공격 판정이 발생하는 시점입니다. 0.85는 마지막 프레임 시작 지점에 해당합니다.")]
    [SerializeField, Range(0f, 1f)] private float attackImpactNormalizedTime = 0.85f;

    [Tooltip("공격 이펙트가 화면에 유지되는 시간입니다.")]
    [SerializeField, Min(0.01f)] private float attackEffectDuration = 0.18f;

    [Tooltip("공격 판정 크기를 기준으로 이펙트 크기를 추가 조절합니다.")]
    [SerializeField] private Vector2 attackEffectSizeMultiplier = Vector2.one;

    [Tooltip("플레이어보다 이펙트를 몇 단계 앞에 표시할지 설정합니다.")]
    [SerializeField] private int attackEffectSortingOrderOffset = 1;

    private Vector2 lastDirection = Vector2.right;
    private int currentDirection = -1;
    private bool currentMovingState = false;
    private float nextAttackTime;
    private bool isAttacking;
    private bool attackImpactTriggered;
    private bool attackFacingLeft;
    private bool isEditMode;
    private Image editModeOverlay;

    private float storedEnvironmentErrorMultiplier = 0f;
    private bool hasStoredEnvironmentError = false;
    private bool hasAcquiredGiantError;
    private int environmentErrorUsesLeft = 0;

    private float storedPlayerErrorMultiplier = 0f;
    private bool hasStoredPlayerError = false;
    private int playerErrorUsesLeft = 0;

    private float playerErrorTimer = 0f;
    private float storedAccelerationErrorMultiplier;
    private bool hasStoredAccelerationError;
    private bool hasAcquiredAccelerationError;
    private float accelerationAttackTimer;
    private float activeAttackSpeedMultiplier = 1f;
    private float baseAnimatorSpeed = 1f;
    private int selectedStoredErrorIndex;
    private bool isSelectingStoredError;

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
        else
        {
            baseAnimatorSpeed = animator.speed;
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

        HandleStoredErrorSelectionInput();

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

        if (!isSelectingStoredError
            && Mouse.current != null
            && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryAttack();
        }

        if (isAttacking)
        {
            UpdateAttackAnimation();
        }

        if (!isAttacking)
        {
            UpdateAnimation(isMoving ? moveDirection : lastDirection, isMoving);
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

        if (accelerationAttackTimer > 0f)
        {
            accelerationAttackTimer -= Time.deltaTime;
            if (accelerationAttackTimer <= 0f)
            {
                accelerationAttackTimer = 0f;
                activeAttackSpeedMultiplier = 1f;
                Debug.Log("가속 오류 전투 Paste 종료");
            }
        }

        UpdateEditModeVisual();
    }

    private void HandleStoredErrorSelectionInput()
    {
        int storedErrorCount = GetStoredErrorCount();

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            if (IsAnyCombatErrorActive())
            {
                isSelectingStoredError = false;
                Debug.Log($"{GetActiveCombatErrorDisplayName()} 효과가 유지되는 동안에는 다른 오류를 사용할 수 없습니다.");
                return;
            }

            if (storedErrorCount == 0)
            {
                Debug.Log("보관함에 사용할 오류가 없습니다.");
                return;
            }

            ClampSelectedStoredErrorIndex();

            if (storedErrorCount == 1)
            {
                ConfirmSelectedStoredError();
                return;
            }

            isSelectingStoredError = true;
            Debug.Log($"오류 선택 시작: {GetStoredErrorDisplayName(GetSelectedStoredErrorType())}");
        }

        if (!isSelectingStoredError)
        {
            return;
        }

        if (Mouse.current != null)
        {
            float scrollY = Mouse.current.scroll.ReadValue().y;
            if (scrollY < 0f)
            {
                selectedStoredErrorIndex = (selectedStoredErrorIndex + 1) % storedErrorCount;
                Debug.Log($"오류 선택: {GetStoredErrorDisplayName(GetSelectedStoredErrorType())}");
            }
            else if (scrollY > 0f)
            {
                selectedStoredErrorIndex = (selectedStoredErrorIndex - 1 + storedErrorCount) % storedErrorCount;
                Debug.Log($"오류 선택: {GetStoredErrorDisplayName(GetSelectedStoredErrorType())}");
            }
        }

        if (Keyboard.current.qKey.wasReleasedThisFrame)
        {
            isSelectingStoredError = false;
            ConfirmSelectedStoredError();
        }
    }

    private void ConfirmSelectedStoredError()
    {
        if (IsAnyCombatErrorActive())
        {
            Debug.Log($"{GetActiveCombatErrorDisplayName()} 효과가 유지되는 동안에는 다른 오류를 사용할 수 없습니다.");
            return;
        }

        if (GetSelectedStoredErrorType() == StoredErrorType.None)
        {
            Debug.Log("선택할 수 있는 오류가 없습니다.");
            return;
        }

        if (isEditMode)
        {
            TryPasteError();
        }
        else
        {
            TryActivateStoredCombatError();
        }
    }

    private void OnGUI()
    {
        if (!isSelectingStoredError || GetStoredErrorCount() < 2)
        {
            return;
        }

        string selectionText = "오류 선택  |  Q를 놓으면 확정\n";
        int storedErrorCount = GetStoredErrorCount();

        for (int index = 0; index < storedErrorCount; index++)
        {
            string marker = index == selectedStoredErrorIndex ? "▶ " : "   ";
            selectionText += marker + GetStoredErrorDisplayName(GetStoredErrorTypeAtIndex(index)) + "\n";
        }

        selectionText += "마우스 휠로 변경";
        GUI.Box(new Rect(20f, 20f, 260f, 92f), selectionText);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    private void ToggleEditMode()
    {
        isSelectingStoredError = false;
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

        float attackSpeedMultiplier = GetCurrentAttackSpeedMultiplier();
        nextAttackTime = Time.time + attackCooldown / attackSpeedMultiplier;
        isAttacking = true;
        currentMovingState = false;

        Vector2 facing = GetMouseDirection();
        bool facingLeft = facing.x < 0f;
        attackFacingLeft = facingLeft;
        attackImpactTriggered = false;

        animator.SetInteger("direction", 2);
        animator.SetBool("isMoving", false);
        animator.speed = baseAnimatorSpeed * attackSpeedMultiplier;

        animator.Play("Attack_Right", 0, 0f);
        spriteRenderer.flipX = facingLeft;
    }

    private void UpdateAttackAnimation()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.shortNameHash != AttackRightStateHash)
        {
            return;
        }

        if (!attackImpactTriggered && stateInfo.normalizedTime >= attackImpactNormalizedTime)
        {
            TriggerAttackImpact();
        }

        if (!animator.IsInTransition(0) && stateInfo.normalizedTime >= 1f)
        {
            AttackFinished();
        }
    }

    public void AttackFinished()
    {
        if (!isAttacking)
        {
            return;
        }

        isAttacking = false;
        currentMovingState = false;
        animator.speed = baseAnimatorSpeed;
        animator.SetBool("isMoving", false);
        animator.ResetTrigger("Attack");
        animator.Play("Player_Idle", 0, 0f);
    }

    private void TriggerAttackImpact()
    {
        if (attackImpactTriggered)
        {
            return;
        }

        attackImpactTriggered = true;
        AttackHit(attackFacingLeft);
        SpawnAttackEffect(attackFacingLeft);
    }

    private void SpawnAttackEffect(bool facingLeft)
    {
        if (attackEffectSprite == null)
        {
            return;
        }

        float attackScale = GetCurrentAttackScale();
        Vector2 effectCenter = (Vector2)transform.position
            + Vector2.right * (facingLeft ? -1f : 1f) * GetCurrentAttackRange();
        Vector2 targetSize = Vector2.Scale(attackSizeSide * attackScale, attackEffectSizeMultiplier);
        Vector2 spriteSize = attackEffectSprite.bounds.size;

        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return;
        }

        GameObject effectObject = new GameObject("PlayerAttackEffect");
        SpriteRenderer effectRenderer = effectObject.AddComponent<SpriteRenderer>();
        effectRenderer.sprite = attackEffectSprite;
        effectRenderer.flipX = facingLeft;
        effectRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        effectRenderer.sortingOrder = spriteRenderer.sortingOrder + attackEffectSortingOrderOffset;

        Vector3 effectScale = new Vector3(
            targetSize.x / spriteSize.x,
            targetSize.y / spriteSize.y,
            1f);
        effectObject.transform.localScale = effectScale;

        Vector3 spriteCenter = attackEffectSprite.bounds.center;
        float centerOffsetX = spriteCenter.x * effectScale.x * (facingLeft ? -1f : 1f);
        float centerOffsetY = spriteCenter.y * effectScale.y;
        effectObject.transform.position = (Vector3)effectCenter - new Vector3(centerOffsetX, centerOffsetY, 0f);

        Destroy(effectObject, Mathf.Max(0.01f, attackEffectDuration));
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

            if (hit.TryGetComponent<AccelerationErrorEffect>(out var accelerationError)
                && accelerationError.IsActive)
            {
                if (!isEditMode)
                {
                    Debug.Log("일반 상태에서는 가속 오류를 Cut할 수 없습니다.");
                    continue;
                }

                if (hasAcquiredAccelerationError)
                {
                    Debug.Log("가속 오류는 이미 한 번 획득했기 때문에 다시 Cut할 수 없습니다.");
                    continue;
                }

                if (hasStoredAccelerationError || GetStoredErrorCount() >= MaxStoredErrors)
                {
                    Debug.Log("가속 오류가 이미 보관되어 있거나 보관함 2칸이 가득 찼습니다.");
                    continue;
                }

                storedAccelerationErrorMultiplier = accelerationError.CurrentMultiplier;
                hasStoredAccelerationError = true;
                hasAcquiredAccelerationError = true;
                selectedStoredErrorIndex = GetStoredErrorIndex(StoredErrorType.Acceleration);
                accelerationError.ResetAcceleration();
                Debug.Log($"가속 오류를 Cut했습니다. 이동 속도가 정상화되었습니다. ({storedAccelerationErrorMultiplier}배 보관)");
                continue;
            }

            if (hit.TryGetComponent<GiantErrorEffect>(out var giantError)
                && giantError.IsActive)
            {
                if (!isEditMode)
                {
                    Debug.Log("일반 상태에서는 거대화 오류를 저장할 수 없습니다.");
                    continue;
                }

                if (hasAcquiredGiantError)
                {
                    Debug.Log("거대화 오류는 이미 한 번 획득했기 때문에 다시 Cut할 수 없습니다.");
                    continue;
                }

                if (hasStoredEnvironmentError || GetStoredErrorCount() >= MaxStoredErrors)
                {
                    Debug.Log("거대화 오류가 이미 보관되어 있거나 보관함 2칸이 가득 찼습니다.");
                    continue;
                }

                storedEnvironmentErrorMultiplier = giantError.CurrentMultiplier;
                environmentErrorUsesLeft = Mathf.Max(1, maxEnvironmentErrorUses);
                hasStoredEnvironmentError = true;
                hasAcquiredGiantError = true;
                selectedStoredErrorIndex = GetStoredErrorIndex(StoredErrorType.Giant);
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
        StoredErrorType selectedError = GetSelectedStoredErrorType();

        if (selectedError == StoredErrorType.Acceleration)
        {
            TryPasteAccelerationError();
            return;
        }

        if (selectedError != StoredErrorType.Giant)
        {
            Debug.Log("붙여넣을 오류가 없습니다.");
            return;
        }

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

        PasteTarget pasteTarget = targetCollider.GetComponentInParent<PasteTarget>();
        if (pasteTarget == null)
        {
            Debug.Log("PasteTarget이 없는 대상에는 거대화 오류를 붙여넣을 수 없습니다.");
            return;
        }

        if (!GiantErrorEffect.CanPasteTo(pasteTarget.TargetType))
        {
            Debug.Log($"거대화 오류는 {pasteTarget.TargetType} 대상에 붙여넣을 수 없습니다.");
            return;
        }

        if (pasteTarget.TargetType == PasteTargetType.CombatSkill)
        {
            Debug.Log("전투 기술 Paste는 일반 모드에서 Q를 사용해야 합니다.");
            return;
        }

        GameObject targetObject = pasteTarget.gameObject;
        GiantErrorEffect effect = targetObject.GetComponent<GiantErrorEffect>();

        if (effect == null)
        {
            effect = targetObject.AddComponent<GiantErrorEffect>();
        }

        float pastedMultiplier = storedEnvironmentErrorMultiplier;
        effect.Trigger(pastedMultiplier);
        ClearEnvironmentError();
        Debug.Log("환경용 거대화 오류가 붙여넣기 되었습니다. 배수: " + pastedMultiplier + "x");
    }

    private void TryPasteAccelerationError()
    {
        if (!isEditMode || !hasStoredAccelerationError)
        {
            Debug.Log("붙여넣을 가속 오류가 없습니다.");
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
                Debug.Log("커서 위치에 가속 오류를 Paste할 대상이 없습니다.");
                return;
            }

            targetCollider = clickedObject.GetComponent<Collider2D>();
            if (targetCollider == null)
            {
                BoxCollider2D collider = clickedObject.AddComponent<BoxCollider2D>();
                SpriteRenderer clickedRenderer = clickedObject.GetComponent<SpriteRenderer>();
                if (clickedRenderer != null)
                {
                    collider.size = clickedRenderer.bounds.size;
                }

                targetCollider = collider;
            }
        }

        PasteTarget pasteTarget = targetCollider.GetComponentInParent<PasteTarget>();
        if (pasteTarget == null)
        {
            Debug.Log("PasteTarget이 없는 대상에는 가속 오류를 Paste할 수 없습니다.");
            return;
        }

        if (!AccelerationErrorEffect.CanPasteTo(pasteTarget.TargetType))
        {
            Debug.Log($"가속 오류는 {pasteTarget.TargetType} 대상에 Paste할 수 없습니다.");
            return;
        }

        if (pasteTarget.TargetType == PasteTargetType.CombatSkill)
        {
            Debug.Log("전투 기술 Paste는 일반 모드에서 Q를 사용해야 합니다.");
            return;
        }

        GameObject targetObject = pasteTarget.gameObject;
        AccelerationErrorEffect accelerationEffect = targetObject.GetComponent<AccelerationErrorEffect>();
        if (accelerationEffect == null)
        {
            accelerationEffect = targetObject.AddComponent<AccelerationErrorEffect>();
        }

        accelerationEffect.Trigger(storedAccelerationErrorMultiplier);
        Debug.Log($"가속 오류를 {targetObject.name}에 Paste했습니다. 이동 속도 {storedAccelerationErrorMultiplier}배");
        ClearStoredAccelerationError();
    }

    private void TryActivateStoredCombatError()
    {
        StoredErrorType selectedError = GetSelectedStoredErrorType();

        if (selectedError == StoredErrorType.Giant)
        {
            TryActivatePlayerErrorBuff();
            return;
        }

        if (selectedError != StoredErrorType.Acceleration)
        {
            Debug.Log("전투 기술에 Paste할 오류가 없습니다.");
            return;
        }

        if (accelerationAttackTimer > 0f)
        {
            Debug.Log("이미 전투 기술에 가속 오류가 적용되어 있습니다.");
            return;
        }

        activeAttackSpeedMultiplier = Mathf.Max(1f, storedAccelerationErrorMultiplier);
        accelerationAttackTimer = accelerationAttackDuration;
        ClearStoredAccelerationError();
        Debug.Log($"가속 오류를 전투 기술에 Paste했습니다. 공격 속도 {activeAttackSpeedMultiplier}배, {accelerationAttackDuration}초 유지");
    }

    private void TryActivatePlayerErrorBuff()
    {
        if (isEditMode)
        {
            Debug.Log("편집 모드에서는 오류를 저장하지 않고, 일반 상태에서만 Q로 버프를 사용할 수 있습니다.");
            return;
        }

        if (!hasStoredEnvironmentError || environmentErrorUsesLeft <= 0)
        {
            Debug.Log("전투 기술에 Paste할 거대화 오류가 없습니다.");
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
        environmentErrorUsesLeft--;

        if (environmentErrorUsesLeft <= 0)
        {
            ClearEnvironmentError();
        }

        Debug.Log("플레이어 거대화 오류 활성화! 공격 범위 " + storedPlayerErrorMultiplier + "배 증가, 지속 시간 " + playerErrorDuration + "초");
    }

    private int GetStoredErrorCount()
    {
        int count = 0;

        if (hasStoredEnvironmentError && environmentErrorUsesLeft > 0)
        {
            count++;
        }

        if (hasStoredAccelerationError)
        {
            count++;
        }

        return count;
    }

    private StoredErrorType GetStoredErrorTypeAtIndex(int index)
    {
        int currentIndex = 0;

        if (hasStoredEnvironmentError && environmentErrorUsesLeft > 0)
        {
            if (index == currentIndex)
            {
                return StoredErrorType.Giant;
            }

            currentIndex++;
        }

        if (hasStoredAccelerationError && index == currentIndex)
        {
            return StoredErrorType.Acceleration;
        }

        return StoredErrorType.None;
    }

    private int GetStoredErrorIndex(StoredErrorType errorType)
    {
        int storedErrorCount = GetStoredErrorCount();

        for (int index = 0; index < storedErrorCount; index++)
        {
            if (GetStoredErrorTypeAtIndex(index) == errorType)
            {
                return index;
            }
        }

        return 0;
    }

    private StoredErrorType GetSelectedStoredErrorType()
    {
        ClampSelectedStoredErrorIndex();
        return GetStoredErrorTypeAtIndex(selectedStoredErrorIndex);
    }

    private void ClampSelectedStoredErrorIndex()
    {
        int storedErrorCount = GetStoredErrorCount();
        selectedStoredErrorIndex = storedErrorCount > 0
            ? Mathf.Clamp(selectedStoredErrorIndex, 0, storedErrorCount - 1)
            : 0;
    }

    private static string GetStoredErrorDisplayName(StoredErrorType errorType)
    {
        return errorType switch
        {
            StoredErrorType.Giant => "[거대화]",
            StoredErrorType.Acceleration => "[가속]",
            _ => "[빈 슬롯]"
        };
    }

    private bool IsAnyCombatErrorActive()
    {
        return playerErrorTimer > 0f || accelerationAttackTimer > 0f;
    }

    private string GetActiveCombatErrorDisplayName()
    {
        if (playerErrorTimer > 0f)
        {
            return "[거대화]";
        }

        if (accelerationAttackTimer > 0f)
        {
            return "[가속]";
        }

        return "[오류]";
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

    private float GetCurrentAttackSpeedMultiplier()
    {
        return accelerationAttackTimer > 0f
            ? Mathf.Max(1f, activeAttackSpeedMultiplier)
            : 1f;
    }

    private void ClearStoredAccelerationError()
    {
        storedAccelerationErrorMultiplier = 0f;
        hasStoredAccelerationError = false;
        RefreshStoredErrorSelection();
    }

    private void ClearEnvironmentError()
    {
        storedEnvironmentErrorMultiplier = 0f;
        environmentErrorUsesLeft = 0;
        hasStoredEnvironmentError = false;
        RefreshStoredErrorSelection();
    }

    private void RefreshStoredErrorSelection()
    {
        ClampSelectedStoredErrorIndex();

        if (GetStoredErrorCount() < 2)
        {
            isSelectingStoredError = false;
        }
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
        Vector2 currentEffectSize = Vector2.Scale(currentAttackSize, attackEffectSizeMultiplier);
        Vector2 rightAttackCenter = center + Vector2.right * currentAttackRange;
        Vector2 leftAttackCenter = center + Vector2.left * currentAttackRange;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireCube(rightAttackCenter, currentAttackSize);
        Gizmos.DrawWireCube(leftAttackCenter, currentAttackSize);

        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.15f);
        Gizmos.DrawCube(rightAttackCenter, currentEffectSize);
        Gizmos.DrawCube(leftAttackCenter, currentEffectSize);

        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.9f);
        Gizmos.DrawWireCube(rightAttackCenter, currentEffectSize);
        Gizmos.DrawWireCube(leftAttackCenter, currentEffectSize);

        if (playerErrorTimer > 0f)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.8f, 0.7f);
            Gizmos.DrawWireCube(rightAttackCenter, currentAttackSize);
            Gizmos.DrawWireCube(leftAttackCenter, currentAttackSize);
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
