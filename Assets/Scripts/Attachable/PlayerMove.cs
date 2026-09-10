using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 플레이어 설정과 실행 순서의 진입점입니다. 기능 구현은 Internal/Player의 partial 파일로 분리됩니다.
/// 플레이어 GameObject에 직접 부착해야 하는 핵심 조작 컴포넌트입니다.
/// </summary>
public partial class PlayerMove : MonoBehaviour, ICombatDamageable
{
    private const int MaxStoredErrors = 2;
    private readonly ErrorInventory storedErrors = new ErrorInventory(MaxStoredErrors);
    private readonly EnvironmentPasteSession environmentPaste = new EnvironmentPasteSession();
    private static readonly int AttackRightStateHash = Animator.StringToHash("Attack_Right");
    private static readonly int AttackUpStateHash = Animator.StringToHash("Attack_Up");
    private static readonly int AttackDownStateHash = Animator.StringToHash("Attack_Down");
    private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
    private static readonly int DirectionHash = Animator.StringToHash("direction");

    [Header("플레이어 기본 설정")]
    [Tooltip("플레이어 이동 속도입니다. 기본 이동 속도를 조절합니다.")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("Player.controller를 연결한 Animator입니다. 이동은 isMoving과 direction(0=위, 1=아래, 2=좌우)으로 전환합니다. 스프라이트 프레임은 Ctrl+6 Animation 창에서 각 .anim 클립을 직접 편집하세요.")]
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

    [Header("전투 기술 - 거대화 오류")]
    [Tooltip("거대화 오류를 전투 기술에 Paste했을 때 공격 범위가 몇 배로 늘어나는지 설정합니다. Paste한 오류는 즉시 보관함에서 사라집니다.")]
    [SerializeField] private float playerErrorEffectMultiplier = 2.5f;

    [Tooltip("거대화 오류를 전투 기술에 1회 Paste했을 때 효과가 유지되는 시간입니다.")]
    [SerializeField] private float playerErrorDuration = 6f;

    [Header("전투 기술 - 가속 오류")]
    [Tooltip("가속 오류를 전투 기술에 1회 Paste했을 때 공격 속도 증가가 유지되는 시간입니다. Paste한 오류는 즉시 보관함에서 사라집니다.")]
    [SerializeField, Min(0.1f)] private float accelerationAttackDuration = 5f;

    [Header("전투 기술 - 반사 오류")]
    [Tooltip("반사 오류를 전투 기술에 1회 Paste했을 때 받은 피해를 공격자에게 되돌리는 시간입니다. Paste한 오류는 즉시 보관함에서 사라집니다.")]
    [SerializeField, Min(0.1f)] private float reflectionCombatDuration = 5f;

    [Header("플레이어 체력")]
    [Tooltip("플레이어가 받을 수 있는 최대 피해량입니다. 반사되지 않은 적 투사체가 이 체력을 감소시킵니다.")]
    [SerializeField, Min(1f)] private float maxHealth = 10f;

    [Tooltip("체력이 0이 됐을 때 테스트를 계속할 수 있도록 최대 체력으로 즉시 복구합니다.")]
    [SerializeField] private bool restoreHealthOnDefeat = true;

    [Header("가드 (우클릭 유지)")]
    [Tooltip("일반 모드에서 우클릭을 누르는 동안 받는 피해를 이 비율(%)만큼 줄입니다. 0=감소 없음, 100=피해 없음. 현재는 피격 방향과 무관하게 적용하며, 반사 오류가 활성화되면 기존 반사 처리가 우선합니다.")]
    [SerializeField, Range(0f, 100f)] private float guardDamageReductionPercent = 50f;

    [Tooltip("가드 게이지 최대치입니다. 가드로 공격을 받을 때마다 10씩 소모하며, 0이 되면 가드가 풀립니다.")]
    [SerializeField, Min(1f)] private float maxGuardGauge = 100f;

    [Tooltip("가드하지 않을 때 초당 자동 회복되는 가드 게이지입니다. 가드 중에는 회복하지 않습니다. 게임 시간 기준입니다.")]
    [SerializeField, Min(0f)] private float guardGaugeRecoveryPerSecond = 20f;

    [Tooltip("화면 왼쪽 아래에 가드 게이지와 현재 상태를 표시합니다.")]
    [SerializeField] private bool showGuardGauge = true;

    [Header("기본 공격")]
    [Tooltip("위/아래 축에서 이 각도 이내의 커서만 세로 공격으로 처리합니다. 기본 22.5도는 8방향 중 위/아래만 세로 공격, 네 대각선은 좌우 공격에 포함합니다. 이펙트와 판정은 상하좌우로만 나갑니다.")]
    [SerializeField, Range(1f, 45f)] private float verticalAttackHalfAngle = 22.5f;

    [Tooltip("근접 공격이 적에게 주는 피해량입니다.")]
    [SerializeField, Min(0.1f)] private float attackDamage = 1f;

    [Tooltip("공격 후 다음 공격까지의 쿨타임입니다.")]
    [SerializeField] private float attackCooldown = 0.35f;

    [Tooltip("기본 공격 범위의 거리입니다. 플레이어 앞쪽으로 얼마나 넓게 공격하는지 설정합니다.")]
    [SerializeField] private float attackRangeSide = 0.75f;

    [Tooltip("공격 판정의 크기입니다. 사각형 판정의 가로/세로 크기를 조절합니다.")]
    [SerializeField] private Vector2 attackSizeSide = new Vector2(0.8f, 0.8f);

    [Tooltip("공격이 감지될 적 레이어입니다. 적 오브젝트가 이 레이어에 있어야 공격 판정에 걸립니다.")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("공격 이펙트")]
    [Tooltip("오른쪽을 향하는 공격 이펙트 이미지를 넣으세요. 클릭 순간 결정한 상하좌우 방향으로만 표시됩니다. 대각선 커서는 좌우 공격으로 처리합니다.")]
    // 설정용 이미지이므로 애니메이션 키 대상에서 제외합니다.
    // Animation 창에 Sprite를 드롭할 때 PlayerMove가 대상 후보로 표시되지 않게 합니다.
    [UnityEngine.Animations.NotKeyable]
    [SerializeField] private Sprite attackEffectSprite;

    [Tooltip("켜면 각 공격 클립의 끝에서 세 번째 프레임이 시작될 때 이펙트와 타격 판정이 함께 발생합니다. 클립 길이와 Samples를 기준으로 자동 계산합니다. 3프레임 이하인 클립은 첫 프레임에 발생합니다.")]
    [UnityEngine.Serialization.FormerlySerializedAs("attackEffectAtAnimationEnd")]
    [UnityEngine.Serialization.FormerlySerializedAs("attackEffectOnLastFrame")]
    [SerializeField] private bool attackEffectOnThirdLastFrame = true;

    [Tooltip("끝에서 세 번째 프레임 자동 계산 옵션을 껐을 때만 사용합니다. 0은 시작, 0.85는 재생 시간의 85%, 1은 끝입니다.")]
    [SerializeField, Range(0f, 1f)] private float attackImpactNormalizedTime = 0.85f;

    [Tooltip("아직 스프라이트를 넣지 않은 빈 공격 클립의 임시 재생 시간입니다. 프레임을 넣으면 실제 클립 길이를 사용합니다.")]
    [SerializeField, Min(0.01f)] private float emptyAttackDuration = 0.3f;

    [Tooltip("공격 이펙트가 화면에 유지되는 시간입니다.")]
    [SerializeField, Min(0.01f)] private float attackEffectDuration = 0.18f;

    [Tooltip("공격 판정 크기를 기준으로 이펙트 크기를 추가 조절합니다.")]
    [SerializeField] private Vector2 attackEffectSizeMultiplier = Vector2.one;

    [Tooltip("플레이어보다 이펙트를 몇 단계 앞에 표시할지 설정합니다.")]
    [SerializeField] private int attackEffectSortingOrderOffset = 1;

    private Vector2 lastDirection = Vector2.right;
    private float nextAttackTime;
    private bool isAttacking;
    private bool attackImpactTriggered;
    private bool suppressErrorCutForCurrentAttack;
    private Vector2 attackDirection = Vector2.right;
    private int currentAttackStateHash;
    private float attackElapsedTime;
    private float currentAttackImpactTime = -1f;
    private bool isEditMode;
    private Image editModeOverlay;


    private float storedPlayerErrorMultiplier = 0f;
    private float playerErrorTimer = 0f;
    private float accelerationAttackTimer;
    private float activeAttackSpeedMultiplier = 1f;
    private float baseAnimatorSpeed = 1f;
    private float currentHealth;
    private float reflectionCombatTimer;
    private int selectedStoredErrorIndex;
    private bool isSelectingStoredError;
    private StoredErrorType pendingReplacementErrorType;
    private MonoBehaviour pendingReplacementSource;
    private float pendingReplacementMultiplier;
    private int selectedReplacementIndex;
    private bool isReplacingStoredError;

    private void Start()
    {
        currentHealth = maxHealth;
        currentGuardGauge = Mathf.Max(1f, maxGuardGauge);

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

        EnsureProjectileCollider();
        CreateEditModeOverlay();
        UpdateEditModeVisual();
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        // 취소/성공한 프레임에도 같은 좌클릭이 Cut 공격으로 이어지지 않도록 입력을 소비합니다.
        bool pasteInputConsumed = environmentPaste.IsBusy;
        if (Keyboard.current[editModeKey].wasPressedThisFrame)
        {
            ToggleEditMode();
        }

        bool replacementInputConsumed = HandleStoredErrorReplacementInput();
        if (!replacementInputConsumed)
        {
            HandleStoredErrorSelectionInput();
        }

        pasteInputConsumed |= environmentPaste.IsBusy;
        if (environmentPaste.IsBusy && isAttacking)
            suppressErrorCutForCurrentAttack = true;
        if (!replacementInputConsumed)
            pasteInputConsumed |= HandleEnvironmentPasteClick();

        if (animator == null || spriteRenderer == null)
        {
            UpdateEditModeVisual();
            return;
        }

        Vector2 moveDirection = GetMovementInput();
        UpdateGuardState();
        bool isMoving = moveDirection != Vector2.zero && !isGuarding;

        if (isMoving)
        {
            moveDirection = moveDirection.normalized;
            lastDirection = moveDirection;
            transform.position += (Vector3)(moveDirection * moveSpeed * Time.deltaTime);
        }

        if (!isSelectingStoredError
            && !isReplacingStoredError
            && !replacementInputConsumed
            && !pasteInputConsumed
            && Mouse.current != null
            && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryAttack();
        }

        if (isAttacking)
        {
            UpdateAttackAnimation();
        }

        // 공격을 끝낸 프레임에도 우클릭을 유지 중이면 즉시 가드로 이어집니다.
        UpdateGuardState();
        if (!isAttacking && !isGuarding)
        {
            UpdateAnimation(isMoving ? moveDirection : lastDirection, isMoving);
        }

        UpdateCombatErrorTimers();
        UpdateGuardGauge(Time.deltaTime);

        UpdateEditModeVisual();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}
