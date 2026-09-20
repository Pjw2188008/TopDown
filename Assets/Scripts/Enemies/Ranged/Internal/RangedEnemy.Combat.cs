using UnityEngine;

/// <summary>
/// RangedEnemy의 공격 시작, 애니메이션 길이, 한 번의 발사 시점과 경직 시 취소를 담당합니다.
/// 직접 부착하지 않는 partial 구현입니다. Inspector 설정은 RangedEnemy.cs에 있습니다.
/// </summary>
public sealed partial class RangedEnemy
{
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private const string AttackClipName = "Ranged_Attack";
    private float nextAttackAt;
    private float attackElapsed;
    private float attackDuration;
    private bool attacking;
    private bool shotReleased;
    private Vector2 shotDirection = Vector2.right;

    private void BeginAttack(Vector2 aim)
    {
        attacking = true;
        shotReleased = false;
        attackElapsed = 0;
        shotDirection = aim.sqrMagnitude > .0001f
            ? aim.normalized
            : (visual.flipX ? Vector2.left : Vector2.right);
        attackDuration = GetAttackDuration();
        if (HasAnimator)
        {
            animator.ResetTrigger(AttackHash);
            animator.SetTrigger(AttackHash);
        }

        nextAttackAt = Time.time + Mathf.Max(.1f, attackInterval);
        SetMoving(false);
        Face(shotDirection);
    }

    private float GetAttackDuration()
    {
        float duration = Mathf.Max(.05f, fallbackAttackDuration);
        if (!HasAnimator) return duration;

        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name != AttackClipName || clip.length <= 0f) continue;
            duration = clip.length;
            break;
        }
        return duration / Mathf.Max(.01f, animator.speed);
    }

    private void AdvanceAttack(float deltaTime)
    {
        attackElapsed += deltaTime;
        if (!shotReleased && attackElapsed >= attackDuration * Mathf.Clamp01(releaseNormalizedTime))
        {
            shotReleased = true;
            // 공격 도중 범위를 벗어난 대상에게는 새 탄환을 발사하지 않습니다.
            if (target != null && Vector2.Distance(target.position, transform.position) <= Mathf.Max(.1f, attackRange))
            {
                Fire();
            }
        }
        if (attackElapsed >= attackDuration) attacking = false;
    }

    private void CancelAttack()
    {
        if (!attacking) return;

        attacking = false;
        shotReleased = true;
        if (HasAnimator)
        {
            animator.ResetTrigger(AttackHash);
            animator.Play("Idle", 0, 0);
        }
    }
}
