using UnityEngine;

/// <summary>편집 가능한 근접 몬스터 Animator 연동입니다. partial 파일이므로 직접 붙이지 않습니다.</summary>
public partial class EnemyController
{
    [Header("근접 몬스터 애니메이션")]
    [Tooltip("새 MeleeMonster 프리팹에서 켭니다. 기존 몬스터에는 기본 OFF로 기존 동작을 유지합니다. Animator의 IsMoving/Stunned(bool), Attack(trigger)을 사용합니다.")]
    [SerializeField] private bool useMeleeAnimator;
    [Tooltip("기존 클립 참조를 유지합니다. 실제 타격은 현재 Animator Attack 상태 진행률을 사용하므로 클립 길이/재생 속도 변경도 반영됩니다.")]
    [SerializeField] private AnimationClip meleeAttackClip;
    [Tooltip("공격 클립 중 실제 피해와 이펙트가 나올 지점입니다. 0.65 = 클립의 65%. Animation Event는 넣지 않습니다.")]
    [SerializeField, Range(.01f, 1f)] private float meleeHitNormalizedTime = .65f;
    [Header("전진 찌르기")]
    [Tooltip("공격 초반은 준비, 찌르는 구간은 빠르게 재생하고 공격 방향으로 전진합니다. 가속 오류와는 별개의 기본 공격 연출입니다.")]
    [SerializeField] private bool useThrustAttack = true;
    [Tooltip("찌르기를 시작할 공격 클립 진행률입니다. 타격 지점보다 작아야 합니다.")]
    [SerializeField, Range(0f, .95f)] private float thrustStartNormalizedTime = .4f;
    [Tooltip("준비 동작 재생 배율입니다.")]
    [SerializeField, Min(.1f)] private float thrustPreparationSpeed = .8f;
    [Tooltip("찌르는 구간 재생 배율입니다. 기본 2.5배로 빠르게 찌릅니다.")]
    [SerializeField, Min(.1f)] private float thrustStrikeSpeed = 2.5f;
    [Tooltip("최대 전진 거리입니다. 공격 시작 시 방향/거리를 고정하며 가까운 플레이어를 지나치지 않도록 줄입니다. 벽 앞에서는 멈춥니다.")]
    [SerializeField, Min(0f)] private float thrustDistance = .8f;
    [Header("근접 몬스터 벽 충돌")]
    [Tooltip("새 프리팹은 ON입니다. 걷기/추적 시 벽을 통과하지 않습니다. 복잡한 경로 탐색은 하지 않습니다.")]
    [SerializeField] private bool respectWalls;
    [Tooltip("이동을 막는 레이어입니다. Trigger/자신/플레이어 몸은 제외합니다.")]
    [SerializeField] private LayerMask meleeBlockingLayers = Physics2D.DefaultRaycastLayers;
    private Animator meleeAnimator;
    private Rigidbody2D meleeBody;
    private bool validMeleeAnimator, movedThisFrame;
    private float attackRecoveryRemaining;
    private bool animatedMeleeAttack, observedAttackState;
    private float savedMeleeAnimatorSpeed, thrustPlannedDistance, thrustAppliedDistance;
    private float activeHitTime, activeThrustStart;
    private readonly System.Collections.Generic.List<RaycastHit2D> meleeMoveHits = new System.Collections.Generic.List<RaycastHit2D>();
    private static readonly int MeleeMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int MeleeStunnedHash = Animator.StringToHash("Stunned");
    private static readonly int MeleeAttackHash = Animator.StringToHash("Attack");
    public bool IsAttacking => animatedMeleeAttack || windingUp || attackRecoveryRemaining > 0f;

    private void PrepareMeleePresentation()
    {
        meleeAnimator = GetComponent<Animator>();
        if (useMeleeAnimator && meleeAnimator != null && meleeAnimator.runtimeAnimatorController != null)
        {
            bool move = false, stun = false, attack = false;
            foreach (var parameter in meleeAnimator.parameters)
            {
                move |= parameter.nameHash == MeleeMovingHash && parameter.type == AnimatorControllerParameterType.Bool;
                stun |= parameter.nameHash == MeleeStunnedHash && parameter.type == AnimatorControllerParameterType.Bool;
                attack |= parameter.nameHash == MeleeAttackHash && parameter.type == AnimatorControllerParameterType.Trigger;
            }
            validMeleeAnimator = move && stun && attack;
            if (!validMeleeAnimator) Debug.LogWarning("근접 Animator에 IsMoving/Stunned/Attack 파라미터가 필요합니다. 기존 공격 시간으로 동작합니다.", this);
        }
        meleeBody = GetComponent<Rigidbody2D>();
        if (respectWalls && meleeBody != null)
        {
            meleeBody.bodyType = RigidbodyType2D.Kinematic;
            meleeBody.gravityScale = 0;
            meleeBody.constraints = RigidbodyConstraints2D.FreezeRotation;
            meleeBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            meleeBody.useFullKinematicContacts = true;
        }
    }

    private void LateUpdate()
    {
        if (!validMeleeAnimator || meleeAnimator == null || !meleeAnimator.isActiveAndEnabled)
        { SyncAttackVisual(); return; }
        bool stunned = stagger != null && stagger.IsStunned;
        if (animatedMeleeAttack)
        {
            if (dead || stunned || target == null || !target.isActiveAndEnabled) CancelAttack();
            else if (Time.deltaTime > 0f) UpdateAnimatedMeleeAttack();
            // 타격 콜백에서 패링 경직이 생긴 경우 같은 프레임에 이동/공격을 중지합니다.
            stunned = stagger != null && stagger.IsStunned;
            if (stunned) CancelAttack();
        }
        meleeAnimator.SetBool(MeleeMovingHash, movedThisFrame && !IsAttacking && !stunned && !dead);
        meleeAnimator.SetBool(MeleeStunnedHash, stunned);
        SyncAttackVisual();
    }

    private void StartMeleeAttackAnimation()
    {
        attackRecoveryRemaining = 0;
        if (!validMeleeAnimator || meleeAnimator == null || !meleeAnimator.isActiveAndEnabled) return;
        FaceMelee(swingDirection);
        meleeAnimator.SetBool(MeleeMovingHash, false);
        meleeAnimator.SetBool(MeleeStunnedHash, false);
        meleeAnimator.ResetTrigger(MeleeAttackHash);
        meleeAnimator.SetTrigger(MeleeAttackHash);
        if (!meleeAnimator.HasState(0, Animator.StringToHash("Attack"))) return;
        savedMeleeAnimatorSpeed = meleeAnimator.speed;
        animatedMeleeAttack = true;
        observedAttackState = false;
        activeHitTime = Mathf.Clamp(meleeHitNormalizedTime, .01f, 1f);
        activeThrustStart = Mathf.Clamp(thrustStartNormalizedTime, 0f, activeHitTime - .01f);
        thrustAppliedDistance = 0f;
        float targetDistance = Vector2.Dot((Vector2)target.transform.position - (Vector2)transform.position, swingDirection);
        thrustPlannedDistance = useThrustAttack ? Mathf.Min(Mathf.Max(0f, thrustDistance), Mathf.Max(0f, targetDistance - attackReach)) : 0f;
        meleeAnimator.speed = savedMeleeAnimatorSpeed * (useThrustAttack ? Mathf.Max(.1f, thrustPreparationSpeed) : 1f);
        // 별도 타이머 대신 실제 Attack 상태 진행률을 읽습니다.
        meleeAnimator.ResetTrigger(MeleeAttackHash);
        meleeAnimator.Play("Attack", 0, 0f);
        meleeAnimator.Update(0f);
        observedAttackState = true;
        swingCenter = (Vector2)transform.position + swingDirection * (attackReach + thrustPlannedDistance);
    }

    private void UpdateAnimatedMeleeAttack()
    {
        var state = meleeAnimator.GetCurrentAnimatorStateInfo(0);
        bool inAttack = state.shortNameHash == Animator.StringToHash("Attack");
        if (!inAttack && !observedAttackState) return;
        if (inAttack) observedAttackState = true;
        // 저프레임으로 마지막 구간을 건너뛰어 Idle로 전환되어도 한 번만 타격합니다.
        float progress = inAttack ? state.normalizedTime : 1f;
        if (windingUp)
        {
            float phase = Mathf.InverseLerp(activeThrustStart, activeHitTime, progress);
            float wanted = thrustPlannedDistance * phase * phase; // 전진 속도가 점점 증가합니다.
            float step = Mathf.Max(0f, wanted - thrustAppliedDistance);
            if (step > 0f) MoveMeleeStep(swingDirection * step); // 찌르기는 항상 벽 충돌을 검사합니다.
            thrustAppliedDistance = wanted;
            if (progress >= activeHitTime)
            {
                swingCenter = (Vector2)transform.position + swingDirection * attackReach;
                ResolveAttack(); // 효과와 피해/패링을 동일한 지점에서 한 번만 처리합니다.
            }
        }
        if (progress >= 1f) { RestoreMeleeAttackSpeed(); return; }
        float multiplier = !useThrustAttack ? 1f : progress < activeThrustStart ? Mathf.Max(.1f, thrustPreparationSpeed)
            : progress < activeHitTime ? Mathf.Max(.1f, thrustStrikeSpeed) : 1f;
        meleeAnimator.speed = savedMeleeAnimatorSpeed * multiplier;
    }

    private void RestoreMeleeAttackSpeed()
    {
        if (animatedMeleeAttack && meleeAnimator != null) meleeAnimator.speed = savedMeleeAnimatorSpeed;
        animatedMeleeAttack = false;
        observedAttackState = false;
    }

    private void StopMeleeAttackAnimation()
    {
        RestoreMeleeAttackSpeed();
        if (!validMeleeAnimator || meleeAnimator == null || !meleeAnimator.isActiveAndEnabled) return;
        meleeAnimator.ResetTrigger(MeleeAttackHash);
        meleeAnimator.Play("Idle", 0, 0);
    }

    private void FaceMelee(Vector2 direction)
    {
        if (useMeleeAnimator && bodyRenderer != null && Mathf.Abs(direction.x) > .0001f)
            bodyRenderer.flipX = direction.x < 0;
    }

    private void MoveMeleeStep(Vector2 displacement)
    {
        float wanted = displacement.magnitude;
        if (wanted <= .00001f) return;
        Physics2D.SyncTransforms();
        float allowed = InteractionMotion.AllowedDistance(transform, player, displacement / wanted, wanted, meleeBlockingLayers, meleeMoveHits);
        Vector3 position = transform.position + (Vector3)(displacement / wanted * allowed);
        if (meleeBody != null) meleeBody.position = position;
        transform.position = position;
        Physics2D.SyncTransforms();
    }
}
