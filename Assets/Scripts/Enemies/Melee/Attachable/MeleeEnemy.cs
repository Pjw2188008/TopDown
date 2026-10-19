using System.Collections.Generic;
using UnityEngine;

/// <summary>씬에 직접 배치하는 사이드뷰 근접 몬스터. 별도 이동 AI/스포너를 붙이지 않습니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer), typeof(Animator), typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D), typeof(EnemyStagger))]
public sealed class MeleeEnemy : TerritoryMonster
{
    [Header("근접 타격")]
    [Tooltip("오른쪽 기준 타격 박스 중심 오프셋. 왼쪽에서 X만 반전됩니다.")]
    public Vector2 hitOffset = new Vector2(.8f, 0f);
    [Tooltip("실제 타격 박스 크기. 노란 기즈모로 표시합니다.")]
    public Vector2 hitSize = new Vector2(1.4f, 1.5f);
    [Min(0), Tooltip("1부터 시작하는 Attack 타격 프레임. 0이면 Hit Normalized Time을 사용합니다. 실제 재생 클립의 FPS를 기준으로 계산합니다.")]
    public int attackHitFrame;
    [Min(0f), Tooltip("이 근접 공격에만 적용할 패링 입력 허용 시간(초). 길게 누른 가드는 패링이 되지 않습니다. 0이면 플레이어 기본값을 사용합니다.")]
    public float meleeParryWindow = .3f;
    protected override float HitTime(AnimationClip clip)
    {
        if (attackHitFrame <= 0 || clip == null || clip.length <= 0f || clip.frameRate <= 0f) return base.HitTime(clip);
        float lastFrame = Mathf.Max(0f, Mathf.Ceil(clip.length * clip.frameRate) - 1f);
        return Mathf.Min(attackHitFrame - 1f, lastFrame) / (clip.frameRate * clip.length);
    }
    protected override void ReleaseAttack()
    {
        if (Target == null) return;
        Vector2 center = (Vector2)transform.position + new Vector2(hitOffset.x * FacingSign, hitOffset.y);
        foreach (var shape in Physics2D.OverlapBoxAll(center, SafeSize(hitSize), 0f))
            if (shape.GetComponentInParent<PlayerMove>() == Target)
            { Target.ReceiveMeleeAttack(attackDamage, gameObject, Mathf.Max(0f, meleeParryWindow)); break; }
    }
    protected override void DrawAttackGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube((Vector2)transform.position + new Vector2(hitOffset.x * FacingSign, hitOffset.y), SafeSize(hitSize));
    }
}

public enum MonsterAggroMode { WhenDamagedByPlayer, WhenPlayerEntersArea }
public enum MonsterBehaviourState { Idle, Chasing, Attacking, Returning, Dead, Patrolling }

/// <summary>두 몬스터가 공유하는 구현입니다. abstract이므로 별도로 붙일 수 없습니다.
/// Animator/클립을 생성하지 않고 기존 상태만 재생합니다.</summary>
public abstract class TerritoryMonster : MonoBehaviour, ICombatDamageable, IAccelerationTarget, ICombatParryReceiver
{
    [Header("추적 방식 / 고정 영역")]
    [Tooltip("When Damaged By Player: 영역 안에서 플레이어에게 맞으면 추적. When Player Enters Area: 플레이어가 영역에 들어오면 추적.")]
    public MonsterAggroMode aggroMode = MonsterAggroMode.WhenPlayerEntersArea;
    [Tooltip("최초 배치 위치 기준 영역 중심 오프셋. 몬스터 이동과 무관하게 고정됩니다.")]
    public Vector2 chaseAreaOffset;
    [Tooltip("고정 영역의 전체 가로/세로(월드 단위). 플레이어가 벗어나면 공격을 취소하고 배치 위치로 복귀합니다.")]
    public Vector2 chaseAreaSize = new Vector2(12f, 6f);
    [Tooltip("비우면 같은 씬의 PlayerMove를 찾습니다. 스포너는 필요 없습니다.")]
    public Transform target;
    [Min(.01f), Tooltip("복귀 완료로 인정할 가로 거리. 복귀 중에는 다시 추적하지 않습니다.")]
    public float returnTolerance = .05f;
    [Header("영역 안 순찰")]
    [Tooltip("추적하지 않을 때 고정 추적 영역 안을 좌우로 순찰합니다. 복귀 완료 후에도 다시 순찰합니다.")]
    public bool patrolEnabled = true;
    [Range(0f, 1f), Tooltip("이동 속도에 곱할 순찰 속도 비율입니다.")]
    public float patrolSpeedMultiplier = .5f;
    [Min(0f), Tooltip("영역 끝, 벽 또는 낭떠러지에서 방향을 바꾸기 전 대기 시간입니다.")]
    public float patrolWaitTime = .6f;
    [Min(0f), Tooltip("몸 Collider와 순찰 영역 끝 사이에 남길 여유 거리입니다.")]
    public float patrolEdgePadding = .15f;
    [Header("이동 / 체력")]
    [Min(0f)] public float moveSpeed = 2f;
    [Min(1f)] public float maxHealth = 10f;
    [Min(0f), Tooltip("접근을 멈출 가로 거리. 공격 범위 이하로 설정합니다.")]
    public float stoppingDistance = .8f;
    [Tooltip("벽/바닥 충돌 레이어. 플레이어 몸은 이동을 막지 않습니다.")]
    public LayerMask movementBlockingLayers = Physics2D.DefaultRaycastLayers;
    [Tooltip("사이드뷰 플레이어와 같은 씬에서 중력을 사용합니다.")]
    public bool useGravity = true;
    [Tooltip("낭떠러지 앞에서 정지합니다. 자동 점프/발판 간 길찾기는 하지 않습니다.")]
    public bool stopAtLedges = true;
    [Header("공격 / 패링")]
    [Min(.1f), Tooltip("공격을 시작할 가로 거리.")] public float attackRange = 1.5f;
    [Min(.1f), Tooltip("공격을 시작할 수 있는 플레이어와의 높이 차이.")] public float attackVerticalRange = 1.5f;
    [Min(0f)] public float attackDamage = 1f;
    [Min(0f), Tooltip("공격 종료 후 다음 공격까지 시간.")] public float attackCooldown = 1f;
    [Range(0f,1f), Tooltip("Attack 클립 중 한 번 타격/발사할 시점.")] public float hitNormalizedTime = .65f;
    [Min(.05f), Tooltip("Attack 클립이 없거나 비었을 때의 공격 시간.")] public float fallbackAttackDuration = .6f;
    [Min(0f)] public float parryStunDuration = 1f;
    [Min(0f)] public float hitStunDuration = .15f;
    [Header("이미지 / Animator (직접 편집)")]
    [Tooltip("Flip X가 꺼진 원본 이미지가 왼쪽을 향하면 체크합니다.")] public bool spriteFacesLeft;
    public string idleState = "Idle";
    public string moveState = "Move";
    public string attackState = "Attack";
    public string hitState = "Hit";
    public string deathState = "Death";
    [Min(.05f)] public float fallbackDeathDuration = .75f;
    public float CurrentHealth { get; private set; }
    public bool IsDead => State == MonsterBehaviourState.Dead;
    public bool IsAttacking => State == MonsterBehaviourState.Attacking;
    public MonsterBehaviourState State { get; private set; }
    public Vector3 HomePosition => initialized ? homePosition : transform.position;
    public Vector2 ChaseAreaCenter => (Vector2)HomePosition + chaseAreaOffset;
    protected PlayerMove Target { get; private set; }
    protected float FacingSign => (Application.isPlaying ? facingLeft : spriteFacesLeft) ? -1f : 1f;
    protected SpriteRenderer Visual { get; private set; }
    private Animator animator;
    private EnemyStagger stagger;
    private Vector3 homePosition;
    private bool initialized, engaged, facingLeft, struck, observedAttack;
    private float multiplier = 1f, nextSearch, cooldown, attackElapsed, hurtUntil, deathElapsed, animationSpeed = 1f;
    private string shownState;
    private readonly EnemyNavigation navigation = new EnemyNavigation();
    private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();
    private readonly List<(Collider2D own, Collider2D other)> ignoredPairs = new List<(Collider2D,Collider2D)>();
    private Transform collisionTarget;
    private float patrolDirection = 1f, patrolWaitRemaining;
    private bool hasMovingParameter, hasStunnedParameter;

    protected virtual void Awake()
    {
        homePosition = transform.position; initialized = true;
        Visual = GetComponent<SpriteRenderer>(); animator = GetComponent<Animator>(); stagger = GetComponent<EnemyStagger>();
        if (stagger == null) stagger = gameObject.AddComponent<EnemyStagger>();
        CurrentHealth = Mathf.Max(1f,maxHealth); facingLeft = spriteFacesLeft ^ (Visual != null && Visual.flipX);
        if (animator != null)
        {
            animationSpeed = animator.speed;
            foreach (var parameter in animator.runtimeAnimatorController != null ? animator.parameters : System.Array.Empty<AnimatorControllerParameter>())
            {
                if (parameter.type != AnimatorControllerParameterType.Bool) continue;
                if (parameter.name == "IsMoving") hasMovingParameter = true;
                if (parameter.name == "Stunned") hasStunnedParameter = true;
            }
        }
        patrolDirection = facingLeft ? -1f : 1f;
        if (TryGetComponent<Rigidbody2D>(out var body))
        { body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0; body.constraints = RigidbodyConstraints2D.FreezeRotation; }
        int layer = LayerMask.NameToLayer("Enemy"); if (gameObject.layer == 0 && layer >= 0) gameObject.layer = layer;
        State = MonsterBehaviourState.Idle; FindTarget(); Play(idleState);
    }
    public void SetTarget(Transform player) { target = player; FindTarget(); }
    public void SetAccelerationMultiplier(float value) { multiplier = Mathf.Max(1f,value); }
    public bool IsInsideChaseArea(Vector3 position)
    {
        var delta = (Vector2)position - ChaseAreaCenter; var half = SafeSize(chaseAreaSize)*.5f;
        return Mathf.Abs(delta.x) <= half.x && Mathf.Abs(delta.y) <= half.y;
    }
    private void FindTarget()
    {
        Target = target != null && target.gameObject.scene == gameObject.scene ? target.GetComponentInParent<PlayerMove>() : null;
        if (Target == null && Time.time >= nextSearch)
        {
            nextSearch = Time.time+.5f;
            foreach (var candidate in FindObjectsByType<PlayerMove>(FindObjectsSortMode.None))
                if (candidate.gameObject.scene == gameObject.scene && candidate.isActiveAndEnabled)
                { Target = candidate; target = candidate.transform; break; }
        }
        if (Target != null) target = Target.transform;
        if (target == collisionTarget) return;
        RestoreCollisions(); collisionTarget = target;
        if (target == null) return;
        foreach (var own in GetComponentsInChildren<Collider2D>())
            foreach (var other in target.GetComponentsInChildren<Collider2D>())
                if (own.enabled && other.enabled && !own.isTrigger && !other.isTrigger && !Physics2D.GetIgnoreCollision(own,other))
                { Physics2D.IgnoreCollision(own,other,true); ignoredPairs.Add((own,other)); }
    }
    protected virtual void Update() => Tick(Time.deltaTime);
    private void Tick(float dt)
    {
        if (dt <= 0f) return;
        if (IsDead)
        {
            deathElapsed += dt; float duration = Mathf.Max(.05f,fallbackDeathDuration);
            if (HasState(deathState))
            {
                var s = animator.GetCurrentAnimatorStateInfo(0);
                if (s.IsName(deathState) && s.length > .01f) duration = s.length/Mathf.Max(.01f,animator.speed);
            }
            if (deathElapsed >= duration) Destroy(gameObject); return;
        }
        FindTarget(); navigation.sideView.stopAtLedges = stopAtLedges;
        if (animator != null) animator.speed = animationSpeed * multiplier;
        if (useGravity) navigation.ApplyGravity(transform,target,dt,movementBlockingLayers);
        cooldown = Mathf.Max(0f,cooldown-dt);
        bool valid = Target != null && Target.isActiveAndEnabled && IsInsideChaseArea(Target.transform.position);
        if ((engaged || IsAttacking) && !valid) BeginReturn();
        if (stagger.IsStunned || Time.time < hurtUntil) { CancelAttack(); Play(hitState); return; }
        if (State == MonsterBehaviourState.Returning)
        {
            if (Mathf.Abs(transform.position.x-homePosition.x) <= Mathf.Max(.01f,returnTolerance))
            { engaged = false; State = MonsterBehaviourState.Idle; patrolWaitRemaining = patrolWaitTime; Play(idleState); }
            else MoveTo(homePosition.x,dt);
            return;
        }
        if (!engaged && valid && aggroMode == MonsterAggroMode.WhenPlayerEntersArea) engaged = true;
        if (!engaged || !valid) { Patrol(dt); return; }
        if (IsAttacking) { attackElapsed += dt*multiplier; return; }
        State = MonsterBehaviourState.Chasing; Face(target.position.x-transform.position.x);
        if (CanAttackTarget() && cooldown <= 0f)
        {
            State = MonsterBehaviourState.Attacking; attackElapsed = 0f; struck = false; observedAttack = false;
            Play(attackState,true); return;
        }
        float difference = target.position.x-transform.position.x;
        float stop = Mathf.Min(Mathf.Max(0f,stoppingDistance),Mathf.Max(.1f,attackRange));
        if (Mathf.Abs(difference) > stop) MoveTo(target.position.x-Mathf.Sign(difference)*stop,dt);
        else Play(idleState);
    }
    protected virtual void LateUpdate()
    {
        if (Visual != null) Visual.flipX = facingLeft != spriteFacesLeft;
        if (!IsAttacking || Time.deltaTime <= 0f) return;
        if (Target == null || !Target.isActiveAndEnabled || !IsInsideChaseArea(Target.transform.position)) { BeginReturn(); return; }
        float progress = attackElapsed/Mathf.Max(.05f,fallbackAttackDuration);
        AnimationClip clip = null;
        if (HasState(attackState))
        {
            var s = animator.GetCurrentAnimatorStateInfo(0);
            if (s.IsName(attackState))
            {
                observedAttack = true;
                var clips = animator.GetCurrentAnimatorClipInfo(0);
                if (clips.Length > 0) clip = clips[0].clip;
                if (clip != null && clip.length > .01f) progress = s.normalizedTime;
            }
            else
            {
                // Attack에 진입하기 전에는 별도 타이머로 먼저 때리지 않습니다.
                // 피격/전환으로 클립이 중단되었을 때도 강제로 마지막 타격을 만들지 않습니다.
                if (observedAttack || attackElapsed > .5f) CancelAttack();
                return;
            }
        }
        if (!struck && progress >= HitTime(clip))
        {
            struck = true; if (CanAttackTarget()) ReleaseAttack();
            if (!IsAttacking) return;
        }
        if (progress >= 1f) { State = MonsterBehaviourState.Chasing; cooldown = Mathf.Max(0f,attackCooldown); Play(idleState); }
    }
    private bool CanAttackTarget()
    {
        if (Target == null || !Target.isActiveAndEnabled || !IsInsideChaseArea(target.position)) return false;
        Vector2 delta = target.position-transform.position;
        return Mathf.Abs(delta.x) <= Mathf.Max(.1f,attackRange) && Mathf.Abs(delta.y) <= Mathf.Max(.1f,attackVerticalRange)
            && navigation.HasSight(transform,target,target.position,movementBlockingLayers);
    }
    private void Patrol(float dt)
    {
        if (!patrolEnabled || moveSpeed <= 0f || patrolSpeedMultiplier <= 0f)
        { State = MonsterBehaviourState.Idle; Play(idleState); return; }
        State = MonsterBehaviourState.Patrolling;
        if (patrolWaitRemaining > 0f)
        { patrolWaitRemaining = Mathf.Max(0f, patrolWaitRemaining-dt); Play(idleState); return; }
        float bodyMargin = 0f;
        foreach (var shape in GetComponentsInChildren<Collider2D>())
            if (shape.enabled && !shape.isTrigger)
                bodyMargin = Mathf.Max(bodyMargin, shape.bounds.extents.x + Mathf.Abs(shape.bounds.center.x-transform.position.x));
        float half = SafeSize(chaseAreaSize).x*.5f - bodyMargin - Mathf.Max(0f, patrolEdgePadding);
        if (half <= .01f) { Play(idleState); return; }
        float left = ChaseAreaCenter.x-half, right = ChaseAreaCenter.x+half;
        if (transform.position.x < left) patrolDirection = 1f;
        else if (transform.position.x > right) patrolDirection = -1f;
        float goal = patrolDirection > 0f ? right : left;
        float moved = MoveTo(goal, dt, Mathf.Clamp01(patrolSpeedMultiplier));
        if (Mathf.Abs(goal-transform.position.x) <= .02f || moved < .00001f)
        {
            patrolDirection = -patrolDirection;
            patrolWaitRemaining = Mathf.Max(.05f, patrolWaitTime);
            Play(idleState);
        }
    }
    private float MoveTo(float x,float dt,float speedScale = 1f)
    {
        float speed = Mathf.Max(0f,moveSpeed)*multiplier*speedScale;
        float desired = Mathf.Clamp(x-transform.position.x,-speed*dt,speed*dt);
        if (State != MonsterBehaviourState.Returning)
        {
            float half = SafeSize(chaseAreaSize).x*.5f;
            float position = transform.position.x, left = ChaseAreaCenter.x-half, right = ChaseAreaCenter.x+half;
            if (position < left) desired = Mathf.Max(0f, desired);
            else if (position > right) desired = Mathf.Min(0f, desired);
            else desired = Mathf.Clamp(position+desired,left,right)-position;
        }
        Face(desired);
        Vector2 step = navigation.sideView.ConstrainStep(transform,target,Vector2.right*desired,movementBlockingLayers);
        float allowed = InteractionMotion.AllowedDistance(transform,target,step.normalized,step.magnitude,movementBlockingLayers,hits,default,true);
        if (allowed > .00001f) { EnemySideView.Move(transform,step.normalized*allowed); Play(moveState); }
        else Play(idleState);
        return allowed;
    }
    private void Face(float x) { if (Mathf.Abs(x) > .001f) facingLeft = x < 0f; }
    private void BeginReturn() { CancelAttack(); engaged = false; State = MonsterBehaviourState.Returning; }
    private void CancelAttack()
    {
        if (!IsAttacking) return;
        State = MonsterBehaviourState.Chasing; struck = true; cooldown = Mathf.Max(0f,attackCooldown); Play(idleState);
    }
    public bool ReceiveDamage(float amount,GameObject source,bool canReflect)
    {
        if (IsDead || !isActiveAndEnabled || amount <= 0f) return false;
        var attacker = source != null ? source.GetComponentInParent<PlayerMove>() : null;
        if (source != null && attacker == null && source.GetComponentInParent<ICombatDamageable>() != null) return false;
        CancelAttack();
        if (attacker != null && State != MonsterBehaviourState.Returning && IsInsideChaseArea(attacker.transform.position))
        { target = attacker.transform; Target = attacker; engaged = true; State = MonsterBehaviourState.Chasing; }
        if (canReflect && source != null && source != gameObject && TryGetComponent<ReflectionErrorEffect>(out var reflection) && reflection.IsActive)
        { CombatDamageUtility.TryApplyDamage(source,amount,gameObject,false); return true; }
        CurrentHealth = Mathf.Max(0f,CurrentHealth-amount); DamageBlink.Play(gameObject);
        if (CurrentHealth <= 0f)
        {
            State = MonsterBehaviourState.Dead; engaged = false; deathElapsed = 0f;
            foreach (var collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
            RestoreCollisions(); Play(deathState,true);
        }
        else { hurtUntil = Time.time+Mathf.Max(0f,hitStunDuration); Play(hitState,true); }
        return true;
    }
    public void TakeDamage(float amount) => ReceiveDamage(amount,null,true);
    public void ReceiveParry(PlayerMove player)
    {
        if (IsDead) return;
        stagger.Stun(parryStunDuration); CancelAttack(); Play(hitState,true);
    }
    private bool HasState(string state) => animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null
        && !string.IsNullOrEmpty(state) && animator.HasState(0,Animator.StringToHash(state));
    private void Play(string state,bool restart = false)
    {
        if (animator != null)
        {
            if (hasMovingParameter) animator.SetBool("IsMoving", state == moveState);
            if (hasStunnedParameter) animator.SetBool("Stunned", state == hitState);
        }
        if (state == hitState && !HasState(state) && HasState("Stagger")) state = "Stagger";
        if (!HasState(state)) return;
        if (!restart && shownState == state && animator.GetCurrentAnimatorStateInfo(0).IsName(state)) return;
        shownState = state; animator.Play(state,0,0f);
    }
    private void RestoreCollisions()
    {
        foreach (var pair in ignoredPairs) if (pair.own != null && pair.other != null) Physics2D.IgnoreCollision(pair.own,pair.other,false);
        ignoredPairs.Clear(); collisionTarget = null;
    }
    protected virtual void OnDisable()
    { CancelAttack(); RestoreCollisions(); if (animator != null) animator.speed = animationSpeed; }
    protected static Vector2 SafeSize(Vector2 size) => new Vector2(Mathf.Max(.1f,size.x),Mathf.Max(.1f,size.y));
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan; Gizmos.DrawWireCube(ChaseAreaCenter,SafeSize(chaseAreaSize));
        Gizmos.color = Color.green; Gizmos.DrawWireSphere(HomePosition,.12f); DrawAttackGizmos();
    }
    protected abstract void ReleaseAttack();
    protected virtual float HitTime(AnimationClip clip) => Mathf.Clamp01(hitNormalizedTime);
    protected abstract void DrawAttackGizmos();
}
