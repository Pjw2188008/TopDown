using UnityEngine;

/// <summary>자동 생성된 파동의 그림 크기와 좌우 반전만 담당합니다. AI가 아니며 직접 붙이지 않습니다.</summary>
public sealed class MeleeWaveVisual : MonoBehaviour
{
    private ReflectProjectile projectile;
    private SpriteRenderer visual;
    private Vector2 size;
    public void Initialize(ReflectProjectile source,Vector2 worldSize)
    {
        projectile = source; visual = GetComponent<SpriteRenderer>();
        size = new Vector2(Mathf.Max(.1f,worldSize.x),Mathf.Max(.1f,worldSize.y));
        Refresh();
    }
    private void LateUpdate() => Refresh();
    private void Refresh()
    {
        if (projectile == null || visual == null || visual.sprite == null) return;
        visual.flipX = projectile.TravelDirection.x < 0f;
        Vector2 spriteSize = visual.sprite.bounds.size;
        transform.localScale = new Vector3(size.x/Mathf.Max(.001f,spriteSize.x),size.y/Mathf.Max(.001f,spriteSize.y),1f);
    }
}
