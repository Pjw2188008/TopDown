using UnityEngine;

/// <summary>보스 Animator 재생과 실제 클립 진행률에 맞춘 타격/발사입니다. 직접 부착하지 않습니다.</summary>
public sealed partial class TutorialBoss
{
    [Header("보스 애니메이션")]
    [SerializeField, HideInInspector] private Animator bossAnimator;
    [SerializeField, Range(.05f,.95f), Tooltip("Slam 클립의 실제 타격 지점입니다. 경고 시간에 이 지점에 도착하도록 재생 속도를 맞춥니다.")] private float slamImpactTime = .65f;
    [SerializeField, Range(.05f,.95f), Tooltip("Shoot 클립의 탄환 발사 지점입니다. 경고 시간에 이 지점에 도착하도록 재생 속도를 맞춥니다.")] private float shotReleaseTime = .65f;
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
    private void PlayBossState(string name)
    {
        if (HasBossAnimator && bossAnimator.HasState(0,Animator.StringToHash(name))) bossAnimator.Play(name,0,0);
    }
    private void BeginBossAttackAnimation()
    {
        if (!HasBossAnimator) return;
        string name = phase == Phase.SlamWarning ? "Slam" : "Shoot";
        int hash = Animator.StringToHash(name);
        if (!bossAnimator.HasState(0,hash)) return;
        savedAnimationSpeed = bossAnimator.speed;
        bossAnimator.Play(hash,0,0);bossAnimator.Update(0);
        var clips = bossAnimator.GetCurrentAnimatorClipInfo(0);
        if (clips.Length == 0 || clips[0].clip == null || clips[0].clip.length <= 0) return;
        animatedPattern = phase;activeAttackHash = hash;
        animationImpact = Mathf.Clamp(phase == Phase.SlamWarning ? slamImpactTime : shotReleaseTime,.05f,.95f);
        attackAnimationActive = true;attackAnimationHit = false;
        // 경고 시간을 유지하면서 길이가 다른 클립에도 재생 속도를 맞춥니다.
        var state = bossAnimator.GetCurrentAnimatorStateInfo(0);
        float stateSpeed = Mathf.Max(.01f,Mathf.Abs(state.speed*state.speedMultiplier));
        bossAnimator.speed = clips[0].clip.length*animationImpact/(Mathf.Max(.1f,phaseDuration)*stateSpeed);
    }
    private void UpdateBossAttackAnimation()
    {
        if (!attackAnimationActive || Time.deltaTime <= 0) return;
        if (!HasBossAnimator)
        {
            StopAttackAnimation();SetPhase(Phase.Recovery,attackCooldown);HideWarning();RestoreBodyPose();return;
        }
        var state = bossAnimator.GetCurrentAnimatorStateInfo(0);
        float progress = state.shortNameHash == activeAttackHash ? state.normalizedTime : 1f;
        if (!attackAnimationHit)
        {
            phaseRemaining = phaseDuration * (1f-Mathf.Clamp01(progress/animationImpact));
            UpdateWarningAndPose();
            if (progress >= animationImpact)
            {
                attackAnimationHit = true;
                SetPhase(Phase.Recovery,attackCooldown);HideWarning();
                if (animatedPattern == Phase.SlamWarning) Slam();else Fire();
                if (!attackAnimationActive) return; // N번째 패링/사망 콜백이 공격을 취소했을 수 있습니다.
            }
        }
        if (progress >= 1f)
        {
            StopAttackAnimation();SetPhase(Phase.Recovery,attackCooldown); // 클립 후딜까지 재생하고 쿨다운을 시작합니다.
        }
    }
    private void StopAttackAnimation()
    {
        if (attackAnimationActive && bossAnimator != null) bossAnimator.speed = savedAnimationSpeed;
        attackAnimationActive = false;
    }
}
