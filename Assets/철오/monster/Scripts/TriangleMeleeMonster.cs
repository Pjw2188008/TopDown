using System.Collections.Generic;
using UnityEngine;

namespace CheolO.Monsters
{
    /// <summary>플레이어에게 접근해 내려찍은 뒤 네 방향 충격파를 발사합니다. 공용 Attack 모션과 좌우 반전을 사용합니다.</summary>
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
        // 이전 씬/프리팹의 반경을 새 사각형의 초기 크기로만 사용합니다.
        [UnityEngine.Serialization.FormerlySerializedAs("attackRange")]
        [HideInInspector, Min(0)] public float stopDistance = .8f;
        [Header("접근 / 정지 사각형")]
        [Tooltip("몬스터 위치에서 공격 시작 사각형 중심까지의 월드 단위 오프셋입니다. 좌우 반전/회전과 무관합니다. Scene의 중심 핸들로 이동할 수도 있습니다.")]
        public Vector2 attackAreaOffset;
        [Tooltip("공격 시작 사각형의 가로/세로 크기(월드 단위)입니다. 0인 축은 이전 Stop Distance의 지름을 사용합니다. 플레이어 Collider가 닿으면 멈춰 패턴을 시작합니다.")]
        public Vector2 attackAreaSize;
        [Header("공격 피해 / 간격")]
        [Min(0)] public float attackDamage = 1f;
        [Min(0)] public float attackCooldown = 1.2f;
        // 기존 씬의 직렬화 값을 보존합니다. 이전 직접 타격 박스는 충격파 공격에서 사용하지 않습니다.
        [HideInInspector]
        [Min(0)] public float attackReach = .75f;
        [HideInInspector] public Vector2 attackSize = new Vector2(1.2f, 1f);
        [Tooltip("추적 중 장애물과 공격 시작 전 시야 확인에 사용하는 레이어입니다.")]
        public LayerMask blockingLayers = 115;

        public enum AttackTimingMode { NormalizedTime, AnimationFrame }
        [Header("내려찍기 / 충격파 발사 타이밍")]
        [Tooltip("켜면 Attack 애니메이션이 끝난 직후 충격파를 한 번 발사합니다. 끄면 아래 비율/프레임 설정을 사용합니다.")]
        public bool releaseAfterAnimation = true;
        [Tooltip("Normalized Time: 클립 진행 비율로 지정. Animation Frame: 공격 클립의 프레임 번호로 지정합니다.")]
        public AttackTimingMode attackTimingMode = AttackTimingMode.NormalizedTime;
        [Tooltip("Normalized Time 모드의 타격 시점. 0 = 시작, 0.5 = 중간, 1 = 끝. 낮추면 빨리, 높이면 늦게 타격합니다. 클립이 없을 때도 이 값을 사용합니다.")]
        [Range(0f, 1f)] public float hitNormalizedTime = .6f;
        [Tooltip("Animation Frame 모드의 타격 프레임(0부터 시작). Animation 창의 프레임 번호를 입력하세요. 클립 끝을 넘으면 마지막에 타격합니다.")]
        [Min(0)] public int hitFrame = 4;
        [Header("상하좌우 충격파")]
        [Tooltip("충격파 이펙트 프리팹. 비우면 Resources/TriangleMonsterEffects/TriangleShockwave를 사용합니다. 본체 Attack 클립은 그대로 사용합니다.")]
        public GameObject shockwaveEffectPrefab;
        [Tooltip("몬스터 위치 기준 발사 중심(월드 단위). 좌우 반전과 관계없이 같은 위치입니다.")]
        public Vector2 shockwaveOriginOffset = new Vector2(0f, .5f);
        [Min(.1f), Tooltip("충격파 이동 속도(초당 월드 단위).")]
        public float shockwaveSpeed = 5f;
        [Min(.1f), Tooltip("최대 비행 거리. 벽이나 피해 대상에 닿으면 먼저 사라집니다.")]
        public float shockwaveDistance = 8f;
        [Min(.01f), Tooltip("충격파 원형 타격 반지름. 그림 크기와 기즈모도 함께 맞춰집니다. 피해량은 Attack Damage입니다.")]
        public float shockwaveRadius = .35f;
        [Tooltip("충격파의 기본 색상. 패링 후에는 공통 이펙트의 패링 색상이 적용됩니다.")]
        public Color shockwaveColor = Color.white;
        [Tooltip("충격파 표시 순서. Sorting Layer는 몬스터 SpriteRenderer를 따릅니다.")]
        public int shockwaveSortingOrder = 30;
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
        private bool facingLeft;
        private Animator animator;
        private SpriteRenderer bodyRenderer;
        private Rigidbody2D body;
        private EnemyStagger stagger;
        private float health, cooldown, elapsed, nextPlayerSearch;
        private bool struck, dead;
        private int stateHash;
        private readonly EnemyNavigation navigation = new EnemyNavigation();
        private readonly List<RaycastHit2D> moveHits = new List<RaycastHit2D>();
        private readonly List<AnimatorClipInfo> attackClips = new List<AnimatorClipInfo>();
        private readonly List<GameObject> shockwaveShots = new List<GameObject>();
        private static Sprite fallbackShockwaveSprite;
        private static readonly Vector2[] ShockwaveDirections = { Vector2.right, Vector2.up, Vector2.left, Vector2.down };
        private static readonly int[] States = BuildStates();
        public float CurrentHealth => health;
        public bool IsDead => dead;
        public Vector2 AttackAreaCenter => (Vector2)transform.position + attackAreaOffset;
        public Vector2 AttackAreaSize => new Vector2(
            attackAreaSize.x > 0f ? attackAreaSize.x : Mathf.Max(.01f, stopDistance * 2f),
            attackAreaSize.y > 0f ? attackAreaSize.y : Mathf.Max(.01f, stopDistance * 2f));

        private static int[] BuildStates()
        {
            var result = new int[5];
            for (int a = 0; a < result.Length; a++)
                result[a] = Animator.StringToHash(((Action)a).ToString());
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
            navigation.ApplyGravity(transform,player != null ? player.transform : null,Time.deltaTime,blockingLayers);
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
            Face(delta);
            // 사각형을 멀리 옮겨도 감지 원 바깥이라는 이유로 공격이 무시되지 않습니다.
            if (IsPlayerInAttackArea() && CanSeePlayer())
            {
                if (cooldown <= 0) { struck = false; Play(Action.Attack, true); }
                else Play(Action.Idle);
                return;
            }
            if (delta.sqrMagnitude > detectionRange * detectionRange)
            {
                Play(Action.Idle);
                return;
            }
            // 위치를 옮긴 사각형이 플레이어에게 닿도록 접근합니다.
            Vector2 approachTarget = (Vector2)player.transform.position - attackAreaOffset;
            Vector2 step = navigation.Step(transform, player.transform, approachTarget,
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
            if (action == Action.Attack && !struck && progress >= AttackHitProgress())
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

        private float AttackHitProgress()
        {
            if (releaseAfterAnimation) return 1f;
            if (attackTimingMode == AttackTimingMode.AnimationFrame
                && animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null
                && animator.GetCurrentAnimatorStateInfo(0).shortNameHash == stateHash)
            {
                animator.GetCurrentAnimatorClipInfo(0, attackClips);
                if (attackClips.Count > 0)
                {
                    AnimationClip clip = attackClips[0].clip;
                    if (clip != null && clip.length > .0001f && clip.frameRate > 0)
                    {
                        // 클립의 프레임을 진행률로 변환하므로 재생 속도가 바뀌어도 같은 모션에서 타격합니다.
                        return Mathf.Clamp01(Mathf.Max(0, hitFrame) / (clip.frameRate * clip.length));
                    }
                }
            }
            return Mathf.Clamp01(hitNormalizedTime);
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
            // 모든 방향에서 공용 클립을 사용합니다. 좌우 반전만으로 클립을 재시작하지 않습니다.
            if (bodyRenderer != null) bodyRenderer.flipX = facingLeft;
            int hash = States[(int)next];
            if (!restart && action == next && stateHash == hash) return;
            action = next; stateHash = hash; elapsed = 0;
            if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null
                && animator.HasState(0, hash)) animator.Play(hash, 0, 0f);
        }

        private void Face(Vector2 delta)
        {
            if (navigation.IsSideView(transform,player != null ? player.transform : null))
            {
                if (Mathf.Abs(delta.x) > .001f) facingLeft = delta.x < 0;
                facing = facingLeft ? Direction.Left : Direction.Right;
                return;
            }
            if (delta.sqrMagnitude < .00001f) return;
            // 수직 이동에서는 마지막 좌우 모습을 유지하고, 공격 판정 방향은 별도로 계산합니다.
            if (Mathf.Abs(delta.x) > .001f) facingLeft = delta.x < 0;
            facing = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                ? (delta.x < 0 ? Direction.Left : Direction.Right)
                : (delta.y < 0 ? Direction.Down : Direction.Up);
        }
        private bool CanSeePlayer() => player != null && navigation.HasSight(transform,
            player.transform, player.transform.position, blockingLayers);

        private bool IsPlayerInAttackArea()
        {
            if (player == null || !player.isActiveAndEnabled) return false;
            Physics2D.SyncTransforms();
            foreach (var hit in Physics2D.OverlapBoxAll(AttackAreaCenter, AttackAreaSize, 0f))
                if (hit.enabled && !hit.isTrigger && hit.GetComponentInParent<PlayerMove>() == player)
                    return true;
            return false;
        }

        private Vector2 ShockwaveOrigin => (Vector2)transform.position + shockwaveOriginOffset;

        // 직접 근접 피해는 주지 않습니다. 발사 뒤 플레이어가 피하거나 패링할 수 있어야 합니다.
        private void Strike()
        {
            if (dead || stagger.IsStunned || player == null || !player.isActiveAndEnabled) return;
            shockwaveShots.RemoveAll(shot => shot == null);
            var prefab = shockwaveEffectPrefab != null ? shockwaveEffectPrefab
                : Resources.Load<GameObject>("TriangleMonsterEffects/TriangleShockwave");
            float radius = Mathf.Max(.01f, shockwaveRadius);
            float speed = Mathf.Max(.1f, shockwaveSpeed);
            foreach (Vector2 direction in ShockwaveDirections)
            {
                var shot = prefab != null ? Instantiate(prefab) : new GameObject("Triangle Shockwave");
                shot.name = "Triangle Shockwave " + direction;
                shot.transform.position = new Vector3(ShockwaveOrigin.x, ShockwaveOrigin.y, transform.position.z);
                var visual = shot.GetComponent<SpriteRenderer>();
                if (visual == null) visual = shot.AddComponent<SpriteRenderer>();
                if (visual.sprite == null)
                {
                    if (fallbackShockwaveSprite == null)
                        fallbackShockwaveSprite = Sprite.Create(Texture2D.whiteTexture,
                            new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(.5f, .5f), 1f);
                    visual.sprite = fallbackShockwaveSprite;
                }
                visual.color = shockwaveColor;
                visual.sortingLayerID = bodyRenderer != null ? bodyRenderer.sortingLayerID : 0;
                visual.sortingOrder = shockwaveSortingOrder;
                var projectile = shot.GetComponent<ReflectProjectile>();
                if (projectile == null) projectile = shot.AddComponent<ReflectProjectile>();
                projectile.PreserveParryReturnLifetime = true;
                projectile.Initialize(gameObject, direction, speed, Mathf.Max(0, attackDamage),
                    radius, Mathf.Max(.1f, shockwaveDistance) / speed, 1, false);
                var effect = shot.GetComponent<BossShockwaveEffect>();
                if (effect == null) effect = shot.AddComponent<BossShockwaveEffect>();
                shot.SetActive(true);
                effect.MatchVisualToHitRadius();
                effect.RefreshDirection();
                shockwaveShots.Add(shot);
            }
        }

        private void ClearShockwaves()
        {
            foreach (var shot in shockwaveShots)
                if (shot != null) { shot.SetActive(false); Destroy(shot); }
            shockwaveShots.Clear();
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
                ClearShockwaves();
                foreach (Collider2D shape in GetComponentsInChildren<Collider2D>()) shape.enabled = false;
                Play(Action.Death, true);
            }
            return true;
        }

        private void OnDisable()
        {
            ClearShockwaves();
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
            Gizmos.DrawWireCube(AttackAreaCenter, AttackAreaSize);
            Gizmos.color = new Color(.5f, .9f, 1f, .8f);
            float radius = Mathf.Max(.01f, shockwaveRadius);
            Gizmos.DrawWireSphere(ShockwaveOrigin, radius);
            foreach (Vector2 direction in ShockwaveDirections)
            {
                Vector2 end = ShockwaveOrigin + direction * Mathf.Max(.1f, shockwaveDistance);
                Gizmos.DrawLine(ShockwaveOrigin, end);
                Gizmos.DrawWireSphere(end, radius);
            }
        }
    }
}
