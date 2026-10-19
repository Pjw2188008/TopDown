using UnityEngine;

/// <summary>직접 배치하는 사이드뷰 원거리 몬스터. 같은 추적/복귀 설정과 공통 투사체 패링을 사용합니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer), typeof(Animator), typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D), typeof(EnemyStagger))]
public sealed class RangedEnemy : TerritoryMonster
{
    [Header("투사체")]
    [Tooltip("투사체 프리팹. 준석 Projectile의 애니메이션/크기/패링도 사용 가능합니다. 비우면 임시 탄환입니다.")]
    public GameObject projectilePrefab;
    [Tooltip("오른쪽 기준 발사 오프셋. 왼쪽에서는 X만 반전됩니다.")]
    public Vector2 muzzleOffset = new Vector2(.6f,0f);
    [Min(.1f)] public float projectileSpeed = 8f;
    [Min(.1f)] public float projectileLifetime = 5f;
    [Min(.01f), Tooltip("Projectile 컴포넌트가 없는 탄환의 타격 반지름. Projectile 프리팹은 해당 컴포넌트의 Hit Radius를 사용합니다.")]
    public float projectileRadius = .12f;
    [Tooltip("켜면 좌우 발사. 끄면 발사 순간 플레이어 위치를 조준하고 이후 직진합니다.")]
    public bool horizontalProjectile;
    [UnityEngine.Animations.NotKeyable] public Sprite projectileSprite;
    public Color projectileColor = Color.white;
    private static Sprite fallbackSprite;
    private void Reset() { attackRange = 6f; stoppingDistance = 4f; attackVerticalRange = 3f; }
    protected override void ReleaseAttack()
    {
        if (Target == null) return;
        Vector2 origin = (Vector2)transform.position + new Vector2(muzzleOffset.x*FacingSign,muzzleOffset.y);
        Vector2 direction = horizontalProjectile ? Vector2.right*FacingSign : ((Vector2)Target.transform.position-origin).normalized;
        if (direction.sqrMagnitude < .001f) direction = Vector2.right*FacingSign;
        GameObject shot = projectilePrefab != null ? Instantiate(projectilePrefab,origin,Quaternion.identity) : new GameObject("Enemy Projectile");
        shot.transform.position = origin; shot.SetActive(true);
        if (shot.TryGetComponent<Projectile>(out var adapter))
        { adapter.Initialize(gameObject,direction,projectileSpeed,attackDamage); return; }
        var renderer = shot.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = shot.AddComponent<SpriteRenderer>();
        if (projectilePrefab == null)
        {
            if (fallbackSprite == null) fallbackSprite = Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
            renderer.sprite = projectileSprite != null ? projectileSprite : fallbackSprite;
            renderer.color = projectileColor;
            renderer.sortingLayerID = Visual.sortingLayerID; renderer.sortingOrder = Visual.sortingOrder+1;
            float size = Mathf.Max(renderer.sprite.bounds.size.x,renderer.sprite.bounds.size.y);
            shot.transform.localScale = Vector3.one*(Mathf.Max(.01f,projectileRadius)*2f/Mathf.Max(.001f,size));
        }
        foreach (var body in shot.GetComponentsInChildren<Rigidbody2D>(true)) body.simulated = false;
        foreach (var shape in shot.GetComponentsInChildren<Collider2D>(true)) shape.enabled = false;
        var projectile = shot.GetComponent<ReflectProjectile>();
        if (projectile == null) projectile = shot.AddComponent<ReflectProjectile>();
        projectile.PreserveParryReturnLifetime = true;
        projectile.Initialize(gameObject,direction,projectileSpeed,attackDamage,projectileRadius,projectileLifetime,1,false);
    }
    protected override void DrawAttackGizmos()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireCube(transform.position,new Vector3(attackRange*2f,attackVerticalRange*2f,0));
        Gizmos.DrawWireSphere((Vector2)transform.position+new Vector2(muzzleOffset.x*FacingSign,muzzleOffset.y),.1f);
    }
}
