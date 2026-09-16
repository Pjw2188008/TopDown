using System.Collections.Generic;
using UnityEngine;

/// <summary>EnemyController의 공격/피해/사망 부분입니다. partial 구현이므로 직접 부착하지 않습니다.</summary>
public partial class EnemyController
{
    private void AttackLogic()
    {
        if (!IsInsideBox(player.position, attackAreaSize)) { currentState = State.Chase; return; }
        if (attackTimer <= 0f) BeginAttack();
    }

    private void BeginAttack()
    {
        if (target == null || dead || stagger.IsStunned) return;
        Vector2 delta = target.transform.position - transform.position;
        swingDirection = delta.sqrMagnitude < .0001f ? Vector2.right
            : Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) ? new Vector2(Mathf.Sign(delta.x), 0f)
            : new Vector2(0f, Mathf.Sign(delta.y));
        swingCenter = (Vector2)transform.position + swingDirection * Mathf.Max(0f, attackReach);
        windingUp = true; windupRemaining = Mathf.Max(.01f, attackWindup);
        effectRemaining = 0f;
        if (showAttackWarning) ShowEffect(true);
        else if (effectObject != null) effectObject.SetActive(false);
    }

    private void ResolveAttack()
    {
        if (!windingUp || dead || stagger.IsStunned || target == null || !target.isActiveAndEnabled) { CancelAttack(); return; }
        windingUp = false; // Consume before callbacks: one swing cannot deal repeated damage.
        attackTimer = Mathf.Max(.01f, attackCooldown); effectRemaining = Mathf.Max(.01f, attackEffectDuration);
        ShowEffect(false); Physics2D.SyncTransforms();
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(swingCenter, HitSize, SwingAngle))
        {
            if (!hit.enabled || hit.isTrigger || hit.GetComponentInParent<PlayerMove>() != target) continue;
            target.ReceiveMeleeAttack(Mathf.Max(0f, attackDamage), gameObject);
            break; // Multiple player colliders still count as one hit.
        }
    }

    public void TakeDamage(float damage) => ReceiveDamage(damage, null, true);

    public bool ReceiveDamage(float amount, GameObject source, bool canReflect)
    {
        if (dead || !isActiveAndEnabled || amount <= 0f) return false;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        Debug.Log($"{name} 피해 {amount} / 남은 체력 {currentHealth}/{maxHealth}", this);
        if (currentHealth <= 0f) Die();
        return true;
    }

    private void Die()
    {
        dead = true; CancelAttack(); RestoreBodyCollisions();
        foreach (Collider2D shape in GetComponentsInChildren<Collider2D>()) shape.enabled = false;
        Destroy(gameObject);
    }
}
