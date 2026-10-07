using UnityEngine;

/// <summary>보스 Animator 재생과 실제 클립 진행률에 맞춘 타격/발사입니다. 직접 부착하지 않습니다.</summary>
public sealed partial class TutorialBoss
{
    [Header("보스 애니메이션")]
    [SerializeField, HideInInspector] private Animator bossAnimator;
    [SerializeField, Tooltip("이동/공격 시작 방향에 따라 SpriteRenderer를 좌우 반전합니다. 공격 시작 뒤와 경직 중에는 방향을 바꾸지 않습니다.")] private bool flipSpriteWithMovement = true;
    [SerializeField, Tooltip("현재 튜토리얼 보스 원본은 왼쪽을 보므로 기본 OFF입니다. Flip X를 끈 원본이 오른쪽인 다른 에셋으로 교체할 때만 켜세요. 이동/공격 반전과 근접 사각형의 좌우 방향에 함께 적용합니다.")] private bool moveSpriteFacesRight = false;
    private int requestedBossState;
    [SerializeField, Min(2), Tooltip("Slam 애니메이션에서 실제 무기를 휘두르는 타격 프레임 번호입니다(첫 프레임=1, 지정은 2부터). 기본 11번째 프레임에 피해/패링을 한 번 판정합니다. 별도 근접 이펙트는 없습니다. 범위를 넘으면 마지막 프레임, 1프레임 클립은 종료 시 타격합니다.")] private int slamHitFrame = 11;
    [SerializeField, Range(.05f,.95f), Tooltip("Shoot 클립의 탄환 발사 지점입니다. Shot Windup Time에 이 지점에 도착하도록 재생 속도를 맞춥니다.")] private float shotReleaseTime = .65f;
    private bool attackAnimationActive, attackAnimationHit;
    private Phase animatedPattern;
    private int activeAttackHash;
    private float animationImpact, savedAnimationSpeed = 1f;
    private bool HasBossAnimator => bossAnimator != null && bossAnimator.isActiveAndEnabled && bossAnimator.runtimeAnimatorController != null;

    private void PrepareAnimation()
    {
        bossAnimator = GetComponent<Animator>();
    }
    private void AnimatePhase(Phase value)
    {
        if (value == Phase.Staggered || value == Phase.Dead || value == Phase.Ready)
        {
            StopAttackAnimation();
            PlayBossState(value == Phase.Staggered ? "Stagger" : "Idle");
        }
        else if (value == Phase.Recovery && !attackAnimationActive) PlayBossState("Idle");
    }
    private void PlayBossState(string name, bool restart = true)
    {
        int hash = Animator.StringToHash(name);
        if (!HasBossAnimator || !bossAnimator.HasState(0,hash)) return;
        if (!restart && requestedBossState == hash) return;
        requestedBossState = hash;
        bossAnimator.Play(hash,0,0);
    }

    private void UpdateMovementAnimation(Vector2 displacement)
    {
        if (attackAnimationActive || (phase != Phase.Ready && phase != Phase.Recovery)) return;
        bool moving = displacement.sqrMagnitude > .00000001f;
        if (moving) FaceAttackDirection(displacement);
        bool hasMove = HasBossAnimator && bossAnimator.HasState(0,Animator.StringToHash("Move"));
        PlayBossState(moving && hasMove ? "Move" : "Idle", false);
    }

    private void FaceAttackDirection(Vector2 direction)
    {
        if (flipSpriteWithMovement && bodyVisual != null && Mathf.Abs(direction.x) > .0001f)
            bodyVisual.flipX = moveSpriteFacesRight ^ (direction.x > 0f) ^ (transform.lossyScale.x < 0f);
    }
    private void BeginBossAttackAnimation()
    {
        if (!HasBossAnimator) return;
        string name = phase == Phase.SlamWarning ? "Slam" : "Shoot";
        int hash = Animator.StringToHash(name);
        if (!bossAnimator.HasState(0,hash)) return;
        savedAnimationSpeed = bossAnimator.speed;
        requestedBossState = hash;
        bossAnimator.Play(hash,0,0);bossAnimator.Update(0);
        var clips = bossAnimator.GetCurrentAnimatorClipInfo(0);
        if (clips.Length == 0 || clips[0].clip == null || clips[0].clip.length <= 0) return;
        animatedPattern = phase;activeAttackHash = hash;
        // 실제 휘두르는 이미지에 판정을 맞추고, 나머지 후딜 프레임도 끝까지 재생합니다.
        animationImpact = phase == Phase.SlamWarning ? GetSlamHitProgress(clips[0].clip) : Mathf.Clamp(shotReleaseTime,.05f,.95f);
        attackAnimationActive = true;attackAnimationHit = false;
        // 준비 시간을 유지하면서 길이가 다른 클립에도 재생 속도를 맞춥니다.
        var state = bossAnimator.GetCurrentAnimatorStateInfo(0);
        float stateSpeed = Mathf.Max(.01f,Mathf.Abs(state.speed*state.speedMultiplier));
        bossAnimator.speed = clips[0].clip.length*animationImpact/(Mathf.Max(.1f,phaseDuration)*stateSpeed);
    }

    private float GetSlamHitProgress(AnimationClip clip)
    {
        float samples = Mathf.Max(1f, clip.length * clip.frameRate);
        int lastFrameIndex = Mathf.Max(0, Mathf.CeilToInt(samples - .0001f) - 1);
        if (lastFrameIndex == 0) return 1f;
        int frameIndex = Mathf.Clamp(slamHitFrame - 1, 1, lastFrameIndex);
        return Mathf.Clamp01(frameIndex / samples);
    }
    private void UpdateBossAttackAnimation()
    {
        if (!attackAnimationActive || Time.deltaTime <= 0) return;
        if (!IsPlayerInRoom())
        {
            StopAttackAnimation();SetPhase(Phase.Recovery,attackCooldown);return;
        }
        if (!HasBossAnimator)
        {
            StopAttackAnimation();SetPhase(Phase.Recovery,attackCooldown);return;
        }
        var state = bossAnimator.GetCurrentAnimatorStateInfo(0);
        if (state.shortNameHash != activeAttackHash)
        {
            // 다른 상태로 중단된 공격을 완료된 공격으로 취급하지 않습니다.
            StopAttackAnimation();SetPhase(Phase.Recovery,attackCooldown);return;
        }
        float progress = state.normalizedTime;
        if (!attackAnimationHit)
        {
            phaseRemaining = phaseDuration * (1f-Mathf.Clamp01(progress/animationImpact));
            if (progress >= animationImpact)
            {
                attackAnimationHit = true;
                SetPhase(Phase.Recovery,attackCooldown);
                if (animatedPattern == Phase.SlamWarning) Slam();else Fire();
                if (!attackAnimationActive) return; // N번째 패링/사망 콜백이 공격을 취소했을 수 있습니다.
            }
        }
        if (progress >= 1f)
        {
            StopAttackAnimation();
            SetPhase(Phase.Recovery,attackCooldown);
        }
    }
    private void StopAttackAnimation()
    {
        if (attackAnimationActive && bossAnimator != null) bossAnimator.speed = savedAnimationSpeed;
        attackAnimationActive = false;
    }
}
