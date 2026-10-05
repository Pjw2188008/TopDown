using UnityEngine;

/// <summary>근접 공격 취소와 편집기 판정 기즈모입니다. 런타임 공격 이펙트는 생성하지 않습니다.</summary>
public partial class EnemyController
{
    private void CancelAttack()
    {
        bool wasAttacking = IsAttacking;
        attackRecoveryRemaining = 0f;
        windingUp = false;
        if (wasAttacking) StopMeleeAttackAnimation();
        EndAttackVisual();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireCube(transform.position, (Vector3)patrolAreaSize);
        Gizmos.color = Color.red; Gizmos.DrawWireCube(transform.position, (Vector3)attackAreaSize);
        Matrix4x4 old = Gizmos.matrix;
        Vector2 center = Application.isPlaying && IsAttacking ? swingCenter
            : (Vector2)transform.position + swingDirection * attackReach;
        Gizmos.matrix = Matrix4x4.TRS((Vector3)center, Quaternion.Euler(0,0,SwingAngle), Vector3.one);
        Gizmos.color = Color.cyan; Gizmos.DrawWireCube(Vector3.zero, (Vector3)HitSize); Gizmos.matrix = old;
    }
}
