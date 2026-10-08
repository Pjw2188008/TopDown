using System.Collections.Generic;
using UnityEngine;

namespace CheolO.Monsters
{
    /// <summary>Triangle 전용 4방향 근접 몬스터. 좌우 공용 상태는 Action, 위/아래는 Action_Up/Down입니다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator), typeof(Rigidbody2D), typeof(BoxCollider2D))]
    [RequireComponent(typeof(EnemyStagger))]
    public sealed class TriangleMeleeMonster : MonoBehaviour, ICombatDamageable
    {
        [Header("대상 / 능력치")]
        [Tooltip("비우면 씬의 PlayerMove를 자동으로 찾습니다.")]
        public PlayerMove player;
        [Min(1)] public float maxHealth = 5f;
        [Min(0)] public float moveSpeed = 2f;
        [Min(0)] public float detectionRange = 8f;
        [Header("접근 / 정지 거리")]
        [Tooltip("플레이어 중심까지의 정지 거리입니다. 실제 타격 범위가 닿지 않으면 더 접근합니다. 청록색 기즈모로 표시됩니다.")]
        [UnityEngine.Serialization.FormerlySerializedAs("attackRange")]
        [Min(0)] public float stopDistance = .8f;
        [Min(0)] public float attackDamage = 1f;
        [Min(0)] public float attackCooldown = 1.2f;
        [Header("공격 판정")]
        [Min(0)] public float attackReach = .75f;
        public Vector2 attackSize = new Vector2(1.2f, 1f);
        [Tooltip("공격 클립의 이 비율에 도달하면 한 번 타격합니다. Animation Event는 필요 없습니다.")]
        [Range(.01f, 1f)] public float hitNormalizedTime = .6f;
        public LayerMask blockingLayers = 115;
        [Header("이미지 연결 전 / 누락된 클립의 임시 재생 시간")]
        [Min(.01f)] public float fallbackAttackDuration = .6f;
        [Min(.01f)] public float fallbackHitDuration = .25f;
        [Min(.01f)] public float fallbackDeathDuration = .75f;
        [Tooltip("죽음 클립 재생 후 오브젝트를 제거합니다. 끄면 마지막 프레임을 유지합니다.")]
        public bool destroyAfterDeath = true;

        private enum Action { Idle, Move, Attack, Hit, Death }
        private enum Direction { Down, Up, Left, Right }
        private Action action;
        private Direction facing = Direction.Down;
        private Animator animator;
        private SpriteRenderer bodyRenderer;
        private Rigidbody2D body;
        private EnemyStagger stagger;
        private float health, cooldown, elapsed, nextPlayerSearch;
        private bool struck, dead;
        private int stateHash;
        private readonly EnemyNavigation navigation = new EnemyNavigation();
        private readonly List<RaycastHit2D> moveHits = new List<RaycastHit2D>();
        private static readonly int[,] States = BuildStates();
        public float CurrentHealth => health;
        public bool IsDead => dead;

        private static int[,] BuildStates()
        {
            var result = new int[5,4];
            for (int a = 0; a < 5; a++)
                for (int d = 0; d < 4; d++)
                    result[a,d] = Animator.StringToHash(((Action)a).ToString()
                        + (d == (int)Direction.Down ? "_Down" : d == (int)Direction.Up ? "_Up" : ""));
            return result;
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            bodyRenderer = GetComponent<SpriteRenderer>();
            body = GetComponent<Rigidbody2D>();
            stagger = GetComponent<EnemyStagger>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            health = Mathf.Max(1, maxHealth);
            Play(Action.Idle, true);
        }

        private void Update()
        {
            if (dead || Time.deltaTime <= 0) return;
            cooldown = Mathf.Max(0, cooldown - Time.deltaTime);
            if (stagger.IsStunned)
            {
                if (action != Action.Hit) Play(Action.Hit, true);
                return;
            }
            if (action == Action.Hit) return;
            if (player == null && Time.time >= nextPlayerSearch)
            {
                player = FindFirstObjectByType<PlayerMove>();
                nextPlayerSearch = Time.time + 1f;
            }
            if (player == null || !player.isActiveAndEnabled)
            {
                Play(Action.Idle);
                return;
            }
            if (action == Action.Attack) return;
            Vector2 delta = player.transform.position - transform.position;
            if (delta.sqrMagnitude > detectionRange * detectionRange)
            {
                Play(Action.Idle);
                return;
            }
            Face(delta);
            if (delta.sqrMagnitude <= stopDistance * stopDistance && IsPlayerInAttackBox() && CanSeePlayer())
            {
                if (cooldown <= 0) { struck = false; Play(Action.Attack, true); }
                else Play(Action.Idle);
                return;
            }
            Vector2 step = navigation.Step(transform, player.transform, player.transform.position,
                Mathf.Max(0, moveSpeed) * Time.deltaTime, blockingLayers);
            float length = step.magnitude;
            if (length <= .00001f) { Play(Action.Idle); return; }
            float allowed = InteractionMotion.AllowedDistance(transform, player.transform,
                step / length, length, blockingLayers, moveHits);
            Vector3 position = transform.position + (Vector3)(step / length * allowed);
            body.position = position;
            transform.position = position;
            if (allowed > .00001f) Face(step);
            Play(allowed > .00001f ? Action.Move : Action.Idle);
        }

        // Animator 평가 뒤 실제 클립 진행률을 사용하므로 이미지 수/FPS를 바꿔도 타격과 종료가 맞습니다.
        private void LateUpdate()
        {
            if (Time.deltaTime <= 0) return;
            elapsed += Time.deltaTime;
            if (action != Action.Attack && action != Action.Hit && action != Action.Death) return;
            float progress = Progress();
            if (action == Action.Attack && !struck && progress >= hitNormalizedTime)
            {
                struck = true; // 패링/반사 콜백 전에 소비하여 중복 타격을 막습니다.
                Strike();
                if (action != Action.Attack) return;
                if (stagger.IsStunned) { Play(Action.Hit, true); return; }
            }
            if (progress < 1f) return;
            if (action == Action.Death)
            {
                if (destroyAfterDeath) Destroy(gameObject);
                return;
            }
            if (action == Action.Hit && stagger.IsStunned) return;
            cooldown = Mathf.Max(cooldown, attackCooldown);
            Play(Action.Idle);
        }

        private float Progress()
        {
            if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.shortNameHash == stateHash && state.length > .0001f)
                    return state.normalizedTime;
            }
            float duration = action == Action.Attack ? fallbackAttackDuration
                : action == Action.Hit ? fallbackHitDuration : fallbackDeathDuration;
            return elapsed / Mathf.Max(.01f, duration);
        }

        private void Play(Action next, bool restart = false)
        {
            // 좌우는 방향 접미사 없는 공용 클립을 공유합니다. 반전만 바꾸므로 이동 클립이 처음부터 재시작되지 않습니다.
            if (bodyRenderer != null) bodyRenderer.flipX = facing == Direction.Left;
            int hash = States[(int)next, (int)facing];
            if (!restart && action == next && stateHash == hash) return;
            action = next; stateHash = hash; elapsed = 0;
            if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null
                && animator.HasState(0, hash)) animator.Play(hash, 0, 0f);
        }

        private void Face(Vector2 delta)
        {
            if (delta.sqrMagnitude < .00001f) return;
            facing = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                ? (delta.x < 0 ? Direction.Left : Direction.Right)
                : (delta.y < 0 ? Direction.Down : Direction.Up);
        }
        private Vector2 FacingVector => facing == Direction.Up ? Vector2.up
            : facing == Direction.Down ? Vector2.down : facing == Direction.Left ? Vector2.left : Vector2.right;
        private bool CanSeePlayer() => player != null && navigation.HasSight(transform,
            player.transform, player.transform.position, blockingLayers);

        private Vector2 AttackBoxSize => new Vector2(Mathf.Max(.01f, attackSize.x), Mathf.Max(.01f, attackSize.y));
        private Vector2 AttackBoxCenter => (Vector2)transform.position + FacingVector * Mathf.Max(0, attackReach);
        private float AttackBoxAngle => Mathf.Atan2(FacingVector.y, FacingVector.x) * Mathf.Rad2Deg;

        // 접근 종료와 실제 타격에 동일한 판정을 사용합니다. 대각선에서 사거리만 보고 멈추는 것을 방지합니다.
        private bool IsPlayerInAttackBox()
        {
            if (player == null || !player.isActiveAndEnabled) return false;
            Physics2D.SyncTransforms();
            foreach (Collider2D hit in Physics2D.OverlapBoxAll(AttackBoxCenter, AttackBoxSize, AttackBoxAngle))
                if (hit.enabled && !hit.isTrigger && hit.GetComponentInParent<PlayerMove>() == player) return true;
            return false;
        }

        private void Strike()
        {
            if (dead || stagger.IsStunned || !IsPlayerInAttackBox() || !CanSeePlayer()) return;
            player.ReceiveMeleeAttack(Mathf.Max(0, attackDamage), gameObject);
        }

        public void TakeDamage(float amount) => ReceiveDamage(amount, null, true);
        public bool ReceiveDamage(float amount, GameObject source, bool canReflect)
        {
            if (dead || !isActiveAndEnabled || amount <= 0) return false;
            health = Mathf.Max(0, health - amount);
            cooldown = Mathf.Max(cooldown, attackCooldown);
            if (health > 0)
            {
                DamageBlink.Play(gameObject);
                Play(Action.Hit, true);
            }
            else
            {
                dead = true;
                foreach (Collider2D shape in GetComponentsInChildren<Collider2D>()) shape.enabled = false;
                Play(Action.Death, true);
            }
            return true;
        }

        private void OnDisable()
        {
            if (!dead) { action = Action.Idle; stateHash = 0; elapsed = 0; }
        }
        private void OnEnable()
        {
            if (animator != null && !dead) Play(Action.Idle, true);
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0, stopDistance));
            Gizmos.color = Color.red;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(AttackBoxCenter, Quaternion.Euler(0, 0, AttackBoxAngle), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, AttackBoxSize);
            Gizmos.matrix = previousMatrix;
        }
    }
}
