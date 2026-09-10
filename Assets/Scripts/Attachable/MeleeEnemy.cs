using UnityEngine;

/// <summary>
/// 패링을 시험할 수 있는 근접 공격 컴포넌트입니다. 적 루트에 직접 부착합니다.
/// 제자리 공격만 담당하며 순찰이 필요하면 같은 오브젝트의 MovingEnemy와 함께 사용합니다.
/// ProjectileEnemy와는 별도 적에 사용하세요(두 체력 수신 컴포넌트의 중복을 방지합니다).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyStagger), typeof(BoxCollider2D))]
public sealed class MeleeEnemy : MonoBehaviour, ICombatDamageable
{
    [Tooltip("공격 대상입니다. 비워두면 PlayerMove를 찾습니다.")]
    [SerializeField] private PlayerMove target;
    [Tooltip("공격을 준비하고 명중할 수 있는 중심 간 거리입니다. 공격 판정 순간에도 다시 확인합니다.")]
    [SerializeField, Min(0.1f)] private float attackRange = 1.5f;
    [Tooltip("공격 준비 시간입니다. 노란 표시가 끝날 때 타격하므로 그 직전 우클릭으로 패링하세요.")]
    [SerializeField, Min(0.01f)] private float windupDuration = 0.4f;
    [Tooltip("타격 이후 다음 공격 준비까지의 대기 시간입니다.")]
    [SerializeField, Min(0.1f)] private float attackCooldown = 1f;
    [Tooltip("근접 공격 피해량입니다.")]
    [SerializeField, Min(0.1f)] private float damage = 2f;
    [Tooltip("테스트 근접 적의 최대 체력입니다.")]
    [SerializeField, Min(1f)] private float maxHealth = 5f;
    private EnemyStagger stagger;
    private float health;
    private bool windingUp;
    private float hitTime;
    private float nextAttackTime;

    private void Awake() { stagger = GetComponent<EnemyStagger>(); health = maxHealth; }
    private void Update()
    {
        if (stagger.IsStunned)
        {
            windingUp = false;
            nextAttackTime = Time.time + attackCooldown;
            return;
        }
        if (target == null) target = FindFirstObjectByType<PlayerMove>();
        if (target == null) { windingUp = false; return; }
        bool inRange = Vector2.Distance(transform.position, target.transform.position) <= attackRange;
        if (windingUp)
        {
            if (Time.time < hitTime) return;
            windingUp = false;
            nextAttackTime = Time.time + attackCooldown;
            if (inRange) target.ReceiveMeleeAttack(damage, gameObject);
        }
        else if (inRange && Time.time >= nextAttackTime)
        {
            windingUp = true;
            hitTime = Time.time + windupDuration;
        }
    }

    public bool ReceiveDamage(float amount, GameObject source, bool canReflect)
    {
        if (amount <= 0f) return false;
        health = Mathf.Max(0f, health - amount);
        if (health <= 0f) Destroy(gameObject);
        return true;
    }

    private void OnGUI()
    {
        if (Camera.main == null || (!windingUp && !stagger.IsStunned)) return;
        Vector3 position = Camera.main.WorldToScreenPoint(transform.position + Vector3.up);
        if (position.z < 0f) return;
        Color previous = GUI.color;
        GUI.color = stagger.IsStunned ? Color.cyan : Color.yellow;
        GUI.Label(new Rect(position.x - 60f, Screen.height - position.y, 150f, 24f),
            stagger.IsStunned ? "첨삭 · 경직" : "! 근접 공격 준비");
        GUI.color = previous;
    }
    private void OnDrawGizmosSelected() { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, attackRange); }
}
