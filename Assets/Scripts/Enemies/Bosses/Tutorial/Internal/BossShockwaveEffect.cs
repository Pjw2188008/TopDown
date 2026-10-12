using UnityEngine;

/// <summary>충격파 프리팹의 방향/실제 판정에 맞는 그림 크기/패링 색상을 관리합니다. 보스가 프리팹을 생성하므로 씬에 따로 붙이지 않습니다.
/// Sprite 프레임은 같은 루트의 Animator가 재생합니다. 별도 Visual 자식이 필요 없습니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer), typeof(Animator), typeof(ReflectProjectile))]
public sealed class BossShockwaveEffect : MonoBehaviour
{
    [SerializeField, Tooltip("원본 이미지가 진행하는 방향 각도입니다. 오른쪽=0, 위쪽=90. 현재 위로 솟는 충격파 이미지 기준 90입니다.")]
    private float spriteForwardAngle = 90f;
    [SerializeField, Tooltip("패링되어 보스에게 돌아갈 때 그림 색상을 변경합니다.")] private bool tintOnParry = true;
    [SerializeField, Tooltip("패링 성공 후 충격파 색상입니다. Sprite 키만 있는 애니메이션을 사용하세요.")]
    private Color parriedColor = new Color(.25f,1f,1f,1f);
    private ReflectProjectile projectile;
    private SpriteRenderer visual;
    private bool fitVisualToHitRadius;

    private void Awake()
    {
        projectile = GetComponent<ReflectProjectile>();
        visual = GetComponent<SpriteRenderer>();
        projectile.PreserveParryReturnLifetime = true;
    }
    private void OnEnable() { projectile.Parried += OnParried; }
    private void OnDisable() { if (projectile != null) projectile.Parried -= OnParried; }
    private void LateUpdate()
    {
        RefreshDirection();
        RefreshVisualSize(); // Animator가 Sprite/Scale 키를 적용한 다음 실제 프레임을 기준으로 맞춥니다.
    }

    /// <summary>보스가 발사 시 호출합니다. 프리팹 미리보기에는 개입하지 않습니다.</summary>
    public void MatchVisualToHitRadius()
    {
        fitVisualToHitRadius = true;
        var animator = GetComponent<Animator>();
        if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
            animator.Update(0f); // 첫 표시부터 프리팹의 임시 Sprite 대신 실제 첫 프레임을 사용합니다.
        RefreshVisualSize();
    }

    private void RefreshVisualSize()
    {
        if (!fitVisualToHitRadius || projectile == null || visual == null || visual.sprite == null) return;
        Vector2 size = visual.sprite.bounds.size;
        float scale = projectile.HitRadius * 2f / Mathf.Max(.001f, size.x, size.y);
        transform.localScale = Vector3.one * scale;
    }
    public void RefreshDirection()
    {
        if (projectile == null || projectile.TravelDirection.sqrMagnitude < .0001f) return;
        Vector2 direction = projectile.TravelDirection;
        transform.rotation = Quaternion.Euler(0f,0f,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-spriteForwardAngle);
    }
    private void OnParried(PlayerMove player)
    {
        if (tintOnParry) visual.color = parriedColor;
        RefreshVisualSize();
        RefreshDirection();
    }
}
