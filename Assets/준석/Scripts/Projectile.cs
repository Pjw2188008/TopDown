using UnityEngine;

/// <summary>준석 투사체 프리팹을 공통 ReflectProjectile에 연결합니다.
/// 이동/충돌/피해/수명은 공통 코드에서만 처리하고 여기서는 설정과 그림 방향을 관리합니다.</summary>
[DisallowMultipleComponent]
public class Projectile : MonoBehaviour
{
    [Min(.1f), Tooltip("일반 비행 수명입니다. 패링하면 발사자에게 돌아갈 시간을 추가 보장합니다.")]
    public float lifeTime = 3f;
    [Min(.01f), Tooltip("월드 단위 타격 반지름. 기존 BoxCollider 대신 연속 CircleCast로 판정합니다.")]
    public float hitRadius = .12f;
    [Header("Visual Size")]
    [Min(.01f), Tooltip("Transform Scale이 1일 때 이미지의 긴 변 크기(월드 단위)입니다. 프레임마다 원본 해상도가 달라도 크기를 맞춥니다. 타격 판정은 Hit Radius로 별도 조절하세요.")]
    public float visualSize = .8f;
    [Tooltip("원본 그림의 진행 방향. 오른쪽=0, 위=90, 왼쪽=180도입니다.")]
    public float spriteForwardAngle;
    [Tooltip("패링 성공 시 적용할 색상입니다.")]
    public Color parriedColor = new Color(.25f, 1f, 1f, 1f);
    private ReflectProjectile projectile;
    private SpriteRenderer visual;
    private float appliedVisualScale = 1f;

    // 기존 호출 호환용입니다. 발사자를 지정해야 해당 적을 향해 패링 반환합니다.
    public void Initialize(Vector2 direction, float speed, float dmg)
        => Initialize(null, direction, speed, dmg);

    public void Initialize(GameObject owner, Vector2 direction, float speed, float dmg)
    {
        projectile = GetComponent<ReflectProjectile>();
        if (projectile == null) projectile = gameObject.AddComponent<ReflectProjectile>();
        projectile.enabled = true;
        projectile.PreserveParryReturnLifetime = true;
        projectile.Initialize(owner, direction, Mathf.Max(.1f, speed), Mathf.Max(0f, dmg),
            Mathf.Max(.01f, hitRadius), Mathf.Max(.1f, lifeTime), 1, false);
        projectile.Parried -= OnParried;
        projectile.Parried += OnParried;
        // 물리 이동/트리거 피해와 공통 이동/피해가 중복되지 않게 합니다.
        foreach (var body in GetComponentsInChildren<Rigidbody2D>(true)) body.simulated = false;
        foreach (var shape in GetComponentsInChildren<Collider2D>(true)) shape.enabled = false;
        visual = GetComponent<SpriteRenderer>();
        RefreshVisualSize();
        RefreshDirection();
    }

    private void LateUpdate()
    {
        RefreshDirection();
        RefreshVisualSize();
    }

    private void RefreshVisualSize()
    {
        if (visual == null) visual = GetComponent<SpriteRenderer>();
        if (visual == null || visual.sprite == null) return;
        Vector3 bounds = visual.sprite.bounds.size;
        float longestSide = Mathf.Max(bounds.x, bounds.y);
        if (longestSide <= .0001f) return;
        float nextScale = Mathf.Max(.01f, visualSize) / longestSide;
        // 이전 프레임 보정만 되돌려 적용하므로 누적 확대되지 않습니다.
        // 사용자가 지정한 Transform Scale 및 외부 오류 배율은 유지합니다.
        transform.localScale *= nextScale / appliedVisualScale;
        appliedVisualScale = nextScale;
    }
    private void OnParried(PlayerMove player)
    {
        if (visual != null) visual.color = parriedColor;
        RefreshDirection();
    }
    private void OnEnable()
    {
        projectile = GetComponent<ReflectProjectile>();
        if (projectile != null) { projectile.Parried -= OnParried; projectile.Parried += OnParried; }
    }
    private void OnDisable()
    {
        if (projectile != null) projectile.Parried -= OnParried;
    }
    private void RefreshDirection()
    {
        if (projectile == null) return;
        Vector2 direction = projectile.TravelDirection;
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - spriteForwardAngle);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(.01f, hitRadius));
    }
}
