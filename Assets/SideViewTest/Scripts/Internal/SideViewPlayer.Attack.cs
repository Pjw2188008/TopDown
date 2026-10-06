using System.Collections.Generic;
using UnityEngine;

/// <summary>사이드뷰 공격 구현입니다. partial 파일이므로 따로 붙이지 않습니다.</summary>
public sealed partial class SideViewPlayer
{
    [Header("사이드뷰 기본 공격")]
    [SerializeField, Min(.01f), Tooltip("좌클릭 공격 한 번의 피해량입니다. 같은 대상의 여러 Collider에 닿아도 한 번만 적용합니다.")]
    private float attackDamage = 1f;
    [SerializeField, Min(0f), Tooltip("공격 애니메이션이 완전히 끝난 뒤 다음 공격까지 대기할 시간입니다.")]
    private float attackCooldown = .15f;
    [SerializeField, Min(0f), Tooltip("플레이어 중심에서 바라보는 방향으로 떨어진 공격 중심 거리입니다(월드 단위).")]
    private float attackReach = 1.1f;
    [SerializeField, Tooltip("공격 사각형의 가로/세로 크기입니다(월드 단위). 루트 Scale과 독립적입니다.")]
    private Vector2 attackSize = new Vector2(1.3f, 1.6f);
    [SerializeField, Tooltip("피해 대상을 찾을 레이어입니다. ICombatDamageable을 구현한 대상에게만 피해를 줍니다.")]
    private LayerMask attackLayers = Physics2D.DefaultRaycastLayers;
    [SerializeField, UnityEngine.Animations.NotKeyable, Tooltip("오른쪽 방향의 공격 이펙트입니다. 왼쪽 공격은 반전합니다. 애니메이션 드롭 후보에는 포함되지 않습니다.")]
    private Sprite attackEffectSprite;
    [SerializeField, Min(.01f), Tooltip("타격 시점부터 공격 이펙트가 보이는 시간입니다.")]
    private float attackEffectDuration = .18f;
    [SerializeField, Min(.05f), Tooltip("Animator/Attack 클립이 없는 경우에만 사용하는 임시 공격 시간입니다.")]
    private float fallbackAttackDuration = .6f;
    [SerializeField, Tooltip("선택했을 때 노란 사각형으로 실제 공격 범위를 표시합니다.")]
    private bool showAttackGizmo = true;

    private static readonly int AttackState = Animator.StringToHash("Base Layer.Attack");
    private readonly HashSet<ICombatDamageable> attackReceivers = new HashSet<ICombatDamageable>();
    private int facing = 1, attackFacing = 1;
    private bool isAttacking, impactTriggered, useAttackAnimation;
    private float attackElapsed, nextAttackTime, impactProgress;
    private GameObject attackEffect;
    public bool IsAttacking => isAttacking;

    private void TryAttack()
    {
        if (!isActiveAndEnabled || isAttacking || Time.timeScale <= 0 || Time.time < nextAttackTime) return;
        isAttacking = true;
        attackFacing = facing;
        horizontal = 0f; moving = false;
        body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        visual.flipX = (attackFacing < 0) == spriteFacesRight;
        attackElapsed = 0f; impactTriggered = false; impactProgress = 2f/3f;
        useAttackAnimation = animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null && animator.HasState(0, AttackState);
        if (useAttackAnimation)
        {
            animator.Play(AttackState, 0, 0f);
            animator.Update(0f);
            var clips = animator.GetCurrentAnimatorClipInfo(0);
            if (clips.Length > 0 && clips[0].clip != null && clips[0].clip.length > 0f)
            {
                var clip = clips[0].clip;
                // 마지막에서 세 번째 프레임: N장이면 시작 위치는 (N-3)/N. 짧은 클립은 처음에 발생합니다.
                impactProgress = Mathf.Clamp01(1f - 3f / Mathf.Max(1f, clip.length * clip.frameRate));
            }
            else useAttackAnimation = false;
        }
    }

    private void LateUpdate()
    {
        if (!isAttacking || Time.timeScale <= 0f) return;
        attackElapsed += Time.deltaTime;
        float progress = attackElapsed / Mathf.Max(.05f, fallbackAttackDuration);
        if (useAttackAnimation)
        {
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
            { CancelAttack(); return; }
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash != AttackState) { CancelAttack(); return; }
            progress = state.normalizedTime;
        }
        if (!impactTriggered && progress >= impactProgress) TriggerImpact();
        if (progress >= 1f) FinishAttack();
    }

    private Vector2 AttackCenter => (Vector2)transform.position + Vector2.right * ((isAttacking ? attackFacing : facing) * attackReach);

    private void TriggerImpact()
    {
        if (impactTriggered) return;
        impactTriggered = true;
        attackReceivers.Clear();
        foreach (var hit in Physics2D.OverlapBoxAll(AttackCenter, attackSize, 0f, attackLayers))
        {
            if (hit.transform.IsChildOf(transform)) continue;
            if (CombatDamageUtility.TryFindReceiver(hit.gameObject, out var receiver) && attackReceivers.Add(receiver))
                receiver.ReceiveDamage(attackDamage, gameObject, false);
        }
        if (attackEffectSprite == null) return;
        if (attackEffect != null) Destroy(attackEffect);
        attackEffect = new GameObject("SideView Attack Effect");
        var renderer = attackEffect.AddComponent<SpriteRenderer>();
        renderer.sprite = attackEffectSprite;renderer.flipX = attackFacing < 0;
        renderer.sharedMaterial = visual.sharedMaterial;renderer.sortingLayerID = visual.sortingLayerID;renderer.sortingOrder = visual.sortingOrder + 1;
        Vector2 bounds = attackEffectSprite.bounds.size;
        Vector3 scale = new Vector3(attackSize.x/Mathf.Max(.001f,bounds.x), attackSize.y/Mathf.Max(.001f,bounds.y), 1f);
        attackEffect.transform.localScale = scale;
        Vector3 offset = Vector3.Scale(attackEffectSprite.bounds.center, scale);if (renderer.flipX) offset.x = -offset.x;
        attackEffect.transform.position = new Vector3(AttackCenter.x, AttackCenter.y, transform.position.z) - offset;
        Destroy(attackEffect, attackEffectDuration);
    }

    private void FinishAttack()
    {
        isAttacking = false;nextAttackTime = Time.time + attackCooldown;
        moving = false;
        if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null && animator.HasState(0,IdleState)) animator.Play(IdleState,0,0f);
    }

    private void CancelAttack()
    {
        if (isAttacking) FinishAttack();
        if (attackEffect != null) Destroy(attackEffect);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showAttackGizmo) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(AttackCenter, new Vector3(attackSize.x, attackSize.y, 0f));
    }
}
