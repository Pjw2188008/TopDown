using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>우클릭 시작 타이밍 판정과 첨삭 결과를 처리합니다. PlayerMove의 partial 파일로 따로 부착하지 않습니다.</summary>
public partial class PlayerMove
{
    private int lastParryInputFrame = -1;
    private float parryDeadline;
    private bool parryAvailable;
    private float parryFeedbackUntil;
    private GUIStyle parryFeedbackStyle;
    private Canvas parryFeedbackCanvas;
    private Image parryFeedbackImage;

    /// <summary>화면 픽셀 단위 UI 영역을 MainCamera의 플레이어 깊이 평면으로 되돌려 표시합니다.</summary>
    private void DrawParryFeedbackGizmo()
    {
        if (!showParryFeedbackGizmo) return;
        Camera viewCamera = Camera.main;
        if (viewCamera == null) return;
        SpriteRenderer playerRenderer = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
        Vector3 feet = transform.position;
        if (playerRenderer != null && playerRenderer.sprite != null)
        {
            Bounds bounds = playerRenderer.bounds;
            feet = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }
        Vector3 screen = viewCamera.WorldToScreenPoint(feet);
        if (screen.z <= 0f) return;

        float width = Mathf.Max(1f, parryFeedbackImageSize.x);
        float height = Mathf.Max(1f, parryFeedbackImageSize.y);
        float top = screen.y - Mathf.Max(0f, parryFeedbackImageOffset);
        Rect area = new Rect(screen.x - width * 0.5f, top - height, width, height);
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = new Color(0.2f, 0.85f, 1f, 1f);
        DrawParryScreenRect(viewCamera, area, screen.z);
        Gizmos.DrawLine(feet, viewCamera.ScreenToWorldPoint(new Vector3(screen.x, top, screen.z)));

        if (parryFeedbackSprite != null)
        {
            // UI Image.preserveAspect와 같은 top-center pivot 기준으로 원본 비율을 맞춥니다.
            Vector2 sourceSize = parryFeedbackSprite.rect.size;
            if (sourceSize.x > 0f && sourceSize.y > 0f)
            {
                float scale = Mathf.Min(width / sourceSize.x, height / sourceSize.y);
                Vector2 fittedSize = sourceSize * scale;
                Rect fitted = new Rect(screen.x - fittedSize.x * 0.5f, top - fittedSize.y,
                    fittedSize.x, fittedSize.y);
                Gizmos.color = Color.yellow;
                DrawParryScreenRect(viewCamera, fitted, screen.z);
            }
        }
        Gizmos.color = previousColor;
        Gizmos.matrix = previousMatrix;
    }

    private static void DrawParryScreenRect(Camera viewCamera, Rect rect, float depth)
    {
        Vector3 bottomLeft = viewCamera.ScreenToWorldPoint(new Vector3(rect.xMin, rect.yMin, depth));
        Vector3 bottomRight = viewCamera.ScreenToWorldPoint(new Vector3(rect.xMax, rect.yMin, depth));
        Vector3 topRight = viewCamera.ScreenToWorldPoint(new Vector3(rect.xMax, rect.yMax, depth));
        Vector3 topLeft = viewCamera.ScreenToWorldPoint(new Vector3(rect.xMin, rect.yMax, depth));
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
    }

    /// <summary>사용자 Sprite를 UI Image로 표시해 Sprite Atlas의 잘린 이미지도 정상 지원합니다.</summary>
    private void UpdateParryFeedbackImage()
    {
        Camera viewCamera = Camera.main;
        bool visible = parryFeedbackSprite != null && Time.time < parryFeedbackUntil
            && isActiveAndEnabled && viewCamera != null;
        if (!visible)
        {
            if (parryFeedbackCanvas != null) parryFeedbackCanvas.gameObject.SetActive(false);
            return;
        }

        Vector3 feet = transform.position;
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Bounds bounds = spriteRenderer.bounds;
            feet = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }
        Vector3 screen = viewCamera.WorldToScreenPoint(feet);
        if (screen.z <= 0f || screen.x < 0f || screen.x > Screen.width
            || screen.y < 0f || screen.y > Screen.height)
        {
            if (parryFeedbackCanvas != null) parryFeedbackCanvas.gameObject.SetActive(false);
            return;
        }

        if (parryFeedbackCanvas == null)
        {
            GameObject canvasObject = new GameObject("ParryFeedbackCanvas", typeof(RectTransform), typeof(Canvas));
            parryFeedbackCanvas = canvasObject.GetComponent<Canvas>();
            parryFeedbackCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            parryFeedbackCanvas.sortingOrder = 101;
            GameObject imageObject = new GameObject("ParryFeedbackImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            parryFeedbackImage = imageObject.GetComponent<Image>();
            parryFeedbackImage.raycastTarget = false;
            parryFeedbackImage.preserveAspect = true;
            RectTransform rect = parryFeedbackImage.rectTransform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 1f);
        }

        parryFeedbackCanvas.gameObject.SetActive(true);
        parryFeedbackImage.sprite = parryFeedbackSprite;
        parryFeedbackImage.rectTransform.sizeDelta = new Vector2(
            Mathf.Max(1f, parryFeedbackImageSize.x), Mathf.Max(1f, parryFeedbackImageSize.y));
        parryFeedbackImage.rectTransform.anchoredPosition = new Vector2(
            screen.x, screen.y - Mathf.Max(0f, parryFeedbackImageOffset));
    }

    /// <summary>가드 HUD와 별개로 플레이어 스프라이트 발밑에 성공 문구를 표시합니다.</summary>
    private void DrawParryFeedback()
    {
        if (parryFeedbackSprite != null || !Application.isPlaying || Time.time >= parryFeedbackUntil) return;
        Camera viewCamera = Camera.main;
        if (viewCamera == null) return;

        Vector3 feet = transform.position;
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Bounds bounds = spriteRenderer.bounds;
            feet = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }
        Vector3 screen = viewCamera.WorldToScreenPoint(feet);
        if (screen.z <= 0f || screen.x < 0f || screen.x > Screen.width
            || screen.y < 0f || screen.y > Screen.height) return;

        if (parryFeedbackStyle == null)
        {
            parryFeedbackStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            parryFeedbackStyle.normal.textColor = Color.white;
        }

        // WorldToScreenPoint는 아래쪽 원점, OnGUI는 위쪽 원점입니다.
        Rect rect = new Rect(screen.x - 90f, Screen.height - screen.y + 8f, 180f, 28f);
        Color previousColor = GUI.color;
        GUI.color = Color.black;
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), "첨삭 성공!", parryFeedbackStyle);
        GUI.color = new Color(0.4f, 0.95f, 1f, 1f);
        GUI.Label(rect, "첨삭 성공!", parryFeedbackStyle);
        GUI.color = previousColor;
    }
    private static readonly int ParryUpStateHash = Animator.StringToHash("Parry_Up");
    private static readonly int ParryDownStateHash = Animator.StringToHash("Parry_Down");
    private static readonly int ParryRightStateHash = Animator.StringToHash("Parry_Right");
    private bool isPlayingParry;
    private int currentParryStateHash;
    private float parryAnimationElapsed;

    private void RefreshParryInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) { parryAvailable = false; return; }
        if (!mouse.rightButton.isPressed || mouse.rightButton.wasReleasedThisFrame)
            guardRequiresRelease = false;

        // 피해가 Player.Update보다 먼저 도착해도 같은 입력을 딱 한 번 평가합니다.
        if (mouse.rightButton.wasPressedThisFrame && lastParryInputFrame != Time.frameCount)
        {
            lastParryInputFrame = Time.frameCount;
            parryAvailable = IsGuardRequested() && !isPlayingParry;
            parryDeadline = Time.time + Mathf.Max(0.01f, parryWindow);
        }
        if (!IsGuardRequested()) parryAvailable = false;
    }

    private bool TryConsumeParry()
    {
        RefreshParryInput();
        if (!parryAvailable || !IsGuardRequested() || Time.time > parryDeadline) return false;
        parryAvailable = false;
        parryFeedbackUntil = Time.time + 0.45f;
        PlayParryAnimation();
        Debug.Log("첨삭 성공! 피해와 가드 게이지 소모를 무효화했습니다.", this);
        return true;
    }

    /// <summary>근접 공격 전용 진입점입니다. 반사 피해나 일반 피해를 근접 패링으로 잘못 처리하지 않습니다.</summary>
    public bool ReceiveMeleeAttack(float amount, GameObject attacker)
    {
        if (amount <= 0f) return false;
        if (attacker != null && TryConsumeParry())
        {
            EnemyStagger stagger = attacker.GetComponentInParent<EnemyStagger>();
            if (stagger != null) stagger.Stun(parryStunDuration);
            return true;
        }
        return ReceiveDamage(amount, attacker, true);
    }

    /// <summary>투사체 충돌 직전에 호출합니다. 성공 후 투사체 자체가 소유자와 진행 방향을 바꿉니다.</summary>
    public bool TryParryProjectile() { return TryConsumeParry(); }

    private void PlayParryAnimation()
    {
        if (animator == null || spriteRenderer == null) return;
        Vector2 direction = GetCardinalAttackDirection(GetMouseDirection());
        int facing = direction.y != 0f ? (direction.y > 0f ? 0 : 1) : 2;
        int stateHash = facing == 0 ? ParryUpStateHash : facing == 1 ? ParryDownStateHash : ParryRightStateHash;
        if (!animator.HasState(0, stateHash))
        {
            Debug.LogWarning("Animator에 Parry_Up / Parry_Down / Parry_Right 상태를 연결해주세요.", this);
            return;
        }
        currentParryStateHash = stateHash;
        parryAnimationElapsed = 0f;
        isPlayingParry = true;
        isGuardFrameHeld = false;
        guardHoldNormalizedTime = -1f;
        currentGuardStateHash = 0;
        animator.speed = baseAnimatorSpeed;
        animator.SetBool(IsMovingHash, false);
        animator.SetInteger(DirectionHash, facing);
        spriteRenderer.flipX = facing == 2 && direction.x < 0f;
        animator.Play(stateHash, 0, 0f);
    }

    private void UpdateParryAnimation()
    {
        if (animator == null) { isPlayingParry = false; return; }
        parryAnimationElapsed += Time.deltaTime * Mathf.Max(0f, baseAnimatorSpeed);
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.shortNameHash != currentParryStateHash) return;
        AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
        bool hasFrames = clips.Length > 0 && clips[0].clip != null && !clips[0].clip.empty;
        bool finished = hasFrames ? state.normalizedTime >= 1f
            : parryAnimationElapsed >= Mathf.Max(0.01f, emptyParryDuration);
        if (!finished) return;

        CancelParryAnimation();
        isGuarding = false;
        currentGuardStateHash = 0;
        UpdateGuardState();
    }

    private void CancelParryAnimation()
    {
        if (!isPlayingParry) return;
        isPlayingParry = false;
        currentParryStateHash = 0;
        parryAnimationElapsed = 0f;
        if (animator != null)
        {
            animator.speed = baseAnimatorSpeed;
            animator.SetBool(IsMovingHash, false);
            animator.Play("Player_Idle", 0, 0f);
        }
    }
}
