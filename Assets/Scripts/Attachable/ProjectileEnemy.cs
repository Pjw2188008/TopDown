using UnityEngine;

/// <summary>
/// 플레이어를 향해 반사 가능한 투사체를 주기적으로 발사하고 적의 체력과 피해 반사를 처리합니다.
/// 투사체를 발사하는 반사 적 GameObject에 직접 부착합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(ReflectionErrorEffect))]
public sealed class ProjectileEnemy : MonoBehaviour, ICombatDamageable
{
    [Header("투사체 공격")]
    [Tooltip("투사체가 추적할 플레이어입니다. 비어 있으면 씬에서 PlayerMove를 자동으로 찾습니다.")]
    [SerializeField] private Transform target;

    [Tooltip("투사체를 발사하는 시간 간격입니다.")]
    [SerializeField, Min(0.1f)] private float fireInterval = 1.5f;

    [Tooltip("생성된 투사체의 이동 속도입니다.")]
    [SerializeField, Min(0.1f)] private float projectileSpeed = 5f;

    [Tooltip("투사체가 플레이어에게 주는 피해량입니다.")]
    [SerializeField, Min(0.1f)] private float projectileDamage = 1f;

    [Tooltip("투사체가 충돌하지 않았을 때 자동으로 사라지는 시간입니다.")]
    [SerializeField, Min(0.1f)] private float projectileLifetime = 6f;

    [Tooltip("반사 오류가 활성화됐을 때 하나의 투사체가 벽이나 물체에서 튕길 수 있는 최대 횟수입니다.")]
    [SerializeField, Min(0)] private int maxProjectileBounces = 2;

    [Tooltip("투사체의 충돌 반지름이자 화면에 표시되는 크기입니다.")]
    [SerializeField, Min(0.03f)] private float projectileRadius = 0.12f;

    [Tooltip("투사체의 표시 색입니다.")]
    [SerializeField] private Color projectileColor = new Color(1f, 0.82f, 0.15f, 1f);

    [Header("체력")]
    [Tooltip("반사 오류를 Cut한 뒤 직접 공격해 처치할 때 사용하는 최대 체력입니다.")]
    [SerializeField, Min(1f)] private float maxHealth = 4f;

    private static Sprite projectileSprite;
    private ReflectionErrorEffect reflectionError;
    private float currentHealth;
    private float nextFireTime;

    private void Awake()
    {
        reflectionError = GetComponent<ReflectionErrorEffect>();
        currentHealth = maxHealth;
    }

    private void Start()
    {
        FindTargetIfNeeded();
        nextFireTime = Time.time + fireInterval;
    }

    private void Update()
    {
        FindTargetIfNeeded();
        if (target == null || Time.time < nextFireTime)
        {
            return;
        }

        FireAtTarget();
        nextFireTime = Time.time + fireInterval;
    }

    public bool ReceiveDamage(float amount, GameObject source, bool canReflect)
    {
        if (amount <= 0f)
        {
            return false;
        }

        if (canReflect && reflectionError != null && reflectionError.IsActive && source != null)
        {
            bool reflected = CombatDamageUtility.TryApplyDamage(source, amount, gameObject, false);
            Debug.Log(reflected
                ? $"{name}이(가) 피해 {amount}을 공격자에게 반사했습니다."
                : $"{name}의 반사 대상이 피해를 받을 수 없습니다.", this);
            return true;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        Debug.Log($"{name} 피해 {amount} / 남은 체력 {currentHealth}/{maxHealth}", this);

        if (currentHealth <= 0f)
        {
            Destroy(gameObject);
        }

        return true;
    }

    private void FindTargetIfNeeded()
    {
        if (target != null)
        {
            return;
        }

        PlayerMove player = FindFirstObjectByType<PlayerMove>();
        if (player != null)
        {
            target = player.transform;
        }
    }

    private void FireAtTarget()
    {
        Vector2 launchDirection = (target.position - transform.position).normalized;
        GameObject projectileObject = new GameObject("Reflection Projectile");
        projectileObject.transform.position = transform.position + (Vector3)(launchDirection * 0.7f);

        SpriteRenderer projectileRenderer = projectileObject.AddComponent<SpriteRenderer>();
        projectileRenderer.sprite = GetProjectileSprite();
        projectileRenderer.color = projectileColor;
        projectileRenderer.sortingOrder = 5;
        projectileObject.transform.localScale = Vector3.one * projectileRadius * 2f;

        CircleCollider2D projectileCollider = projectileObject.AddComponent<CircleCollider2D>();
        projectileCollider.radius = 0.5f;
        projectileCollider.isTrigger = true;

        ReflectProjectile projectile = projectileObject.AddComponent<ReflectProjectile>();
        projectile.Initialize(
            gameObject,
            launchDirection,
            projectileSpeed,
            projectileDamage,
            projectileRadius,
            projectileLifetime,
            maxProjectileBounces,
            reflectionError != null && reflectionError.IsActive);
    }

    private static Sprite GetProjectileSprite()
    {
        if (projectileSprite != null)
        {
            return projectileSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "ReflectionProjectileTexture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        projectileSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        projectileSprite.name = "ReflectionProjectileSprite";
        return projectileSprite;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = projectileColor;
        Gizmos.DrawWireSphere(transform.position, projectileRadius);
    }
}
