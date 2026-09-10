using UnityEngine;

/// <summary>PlayerMove의 공격·피해·이펙트·기즈모 구현 부분입니다. 별도 컴포넌트가 아니므로 직접 부착하지 않습니다.</summary>
public partial class PlayerMove
{
    private void TryAttack()
    {
        if (Time.time < nextAttackTime || isAttacking || isGuarding || isPlayingParry || IsGuardRequested())
        {
            return;
        }

        float attackSpeedMultiplier = GetCurrentAttackSpeedMultiplier();
        nextAttackTime = Time.time + attackCooldown / attackSpeedMultiplier;
        isAttacking = true;
        suppressErrorCutForCurrentAttack = false;

        // 대각선 커서는 좌우에 배정하고, 애니메이션/이펙트/판정을 같은 4방향으로 고정합니다.
        attackDirection = GetCardinalAttackDirection(GetMouseDirection());
        bool verticalAttack = attackDirection.y != 0f;
        int attackAnimationDirection = verticalAttack ? (attackDirection.y > 0f ? 0 : 1) : 2;
        currentAttackStateHash = attackAnimationDirection == 0 ? AttackUpStateHash
            : attackAnimationDirection == 1 ? AttackDownStateHash : AttackRightStateHash;
        attackElapsedTime = 0f;
        currentAttackImpactTime = -1f;
        attackImpactTriggered = false;

        animator.SetInteger(DirectionHash, attackAnimationDirection);
        animator.SetBool(IsMovingHash, false);
        animator.speed = baseAnimatorSpeed * attackSpeedMultiplier;

        animator.Play(currentAttackStateHash, 0, 0f);
        spriteRenderer.flipX = !verticalAttack && attackDirection.x < 0f;
    }

    private void UpdateAttackAnimation()
    {
        attackElapsedTime += Time.deltaTime * Mathf.Max(0f, animator.speed);
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.shortNameHash != currentAttackStateHash)
        {
            return;
        }

        // 빈 클립에서 공격 잠금이 남지 않도록 이미지 설정 전에는 임시 시간을 사용합니다.
        float progress = stateInfo.length > 0.0001f ? stateInfo.normalizedTime
            : attackElapsedTime / Mathf.Max(0.01f, emptyAttackDuration);
        if (currentAttackImpactTime < 0f)
            currentAttackImpactTime = attackEffectOnThirdLastFrame
                ? GetThirdLastAttackFrameNormalizedTime() : attackImpactNormalizedTime;

        if (!attackImpactTriggered && progress >= currentAttackImpactTime)
        {
            TriggerAttackImpact();
        }

        if (!animator.IsInTransition(0) && progress >= 1f)
        {
            AttackFinished();
        }
    }

    private float GetThirdLastAttackFrameNormalizedTime()
    {
        AnimatorClipInfo[] infos = animator.GetCurrentAnimatorClipInfo(0);
        if (infos.Length == 0 || infos[0].clip == null) return 1f;
        return AttackMath.ImpactTime(infos[0].clip.length, infos[0].clip.frameRate);
    }

    public void AttackFinished()
    {
        if (!isAttacking)
        {
            return;
        }

        isAttacking = false;
        animator.speed = baseAnimatorSpeed;
        animator.SetBool(IsMovingHash, false);
        animator.Play("Player_Idle", 0, 0f);
    }

    private void TriggerAttackImpact()
    {
        if (attackImpactTriggered)
        {
            return;
        }

        attackImpactTriggered = true;
        AttackHit();
        SpawnAttackEffect();
    }

    private void SpawnAttackEffect()
    {
        if (attackEffectSprite == null)
        {
            return;
        }

        float attackScale = GetCurrentAttackScale();
        Vector2 effectCenter = (Vector2)transform.position
            + attackDirection * GetCurrentAttackRange();
        Vector2 targetSize = Vector2.Scale(attackSizeSide * attackScale, attackEffectSizeMultiplier);
        Vector2 spriteSize = attackEffectSprite.bounds.size;

        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return;
        }

        GameObject effectObject = new GameObject("PlayerAttackEffect");
        SpriteRenderer effectRenderer = effectObject.AddComponent<SpriteRenderer>();
        effectRenderer.sprite = attackEffectSprite;
        bool facingLeft = attackDirection.x < 0f;
        effectRenderer.flipX = facingLeft;
        effectRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        effectRenderer.sortingOrder = spriteRenderer.sortingOrder + attackEffectSortingOrderOffset;

        Vector3 effectScale = new Vector3(
            targetSize.x / spriteSize.x,
            targetSize.y / spriteSize.y,
            1f);
        effectObject.transform.localScale = effectScale;
        float angle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle - (facingLeft ? 180f : 0f));
        effectObject.transform.rotation = rotation;

        Vector3 spriteCenter = attackEffectSprite.bounds.center;
        float centerOffsetX = spriteCenter.x * effectScale.x * (facingLeft ? -1f : 1f);
        float centerOffsetY = spriteCenter.y * effectScale.y;
        effectObject.transform.position = (Vector3)effectCenter
            - rotation * new Vector3(centerOffsetX, centerOffsetY, 0f);

        Destroy(effectObject, Mathf.Max(0.01f, attackEffectDuration));
    }

    private void AttackHit()
    {
        Vector2 center = (Vector2)transform.position + attackDirection * GetCurrentAttackRange();
        float angle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, attackSizeSide * GetCurrentAttackScale(), angle, enemyLayer);
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == gameObject || TryHandleErrorHit(hit)) continue;
            if (!CombatDamageUtility.TryApplyDamage(hit.gameObject, attackDamage, gameObject))
                Debug.Log("근접 공격 히트: " + hit.name);
        }
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

    private Vector2 GetCardinalAttackDirection(Vector2 direction)
    {
        return AttackMath.CardinalDirection(direction, verticalAttackHalfAngle);
    }

    private void OnDrawGizmosSelected()
    {
        DrawParryFeedbackGizmo();
        if (spriteRenderer == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            DrawAttackGizmo(isAttacking ? attackDirection : GetCardinalAttackDirection(GetMouseDirection()));
        }
        else
        {
            DrawAttackGizmo(Vector2.right);
            DrawAttackGizmo(Vector2.left);
            DrawAttackGizmo(Vector2.up);
            DrawAttackGizmo(Vector2.down);
        }
    }

    private void DrawAttackGizmo(Vector2 direction)
    {
        Vector2 center = (Vector2)transform.position + direction * GetCurrentAttackRange();
        Vector2 size = attackSizeSide * GetCurrentAttackScale();
        Vector2 effectSize = Vector2.Scale(size, attackEffectSizeMultiplier);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;
        Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0f, 0f, angle), Vector3.one);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.7f);
        Gizmos.DrawWireCube(Vector3.zero, size);
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.15f);
        Gizmos.DrawCube(Vector3.zero, effectSize);
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.9f);
        Gizmos.DrawWireCube(Vector3.zero, effectSize);
        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }

    public bool ReceiveDamage(float amount, GameObject source, bool canReflect)
    {
        if (amount <= 0f)
        {
            return false;
        }

        if (canReflect && reflectionCombatTimer > 0f && source != null)
        {
            bool reflected = CombatDamageUtility.TryApplyDamage(source, amount, gameObject, false);
            Debug.Log(reflected
                ? $"반사 오류로 피해 {amount}을 공격자에게 되돌렸습니다."
                : "반사할 공격자가 피해를 받을 수 없습니다.", this);
            return true;
        }

        // 입력을 직접 확인하여 우클릭 해제/편집 모드 전환 직후 남은 상태로 방어하지 않습니다.
        // 반사 오류는 위에서 먼저 처리하므로 가드로 반사 피해량까지 줄이지 않습니다.
        if (IsGuardRequested())
        {
            amount *= 1f - Mathf.Clamp(guardDamageReductionPercent, 0f, 100f) / 100f;
            // 게이지를 소진시킨 타격까지는 가드 감소율을 적용하고, 그다음 타격부터 일반 피해입니다.
            ConsumeGuardGaugeOnHit();
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        Debug.Log($"플레이어 피해 {amount} / 남은 체력 {currentHealth}/{maxHealth}", this);

        if (currentHealth <= 0f && restoreHealthOnDefeat)
        {
            currentHealth = maxHealth;
            Debug.Log("플레이어 체력이 0이 되어 테스트용으로 전부 회복했습니다.", this);
        }

        return true;
    }
}
