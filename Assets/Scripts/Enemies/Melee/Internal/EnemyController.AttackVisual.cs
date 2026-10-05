using UnityEngine;

/// <summary>같은 공격 Sprite를 방향에 맞춰 표시합니다. 그림만 회전하며 몸/Collider/Animator 트랙은 그대로 둡니다.</summary>
public partial class EnemyController
{
    [Header("공격 이미지 방향")]
    [Tooltip("오른쪽을 향한 공격 이미지를 재사용합니다. 위는 +90도, 아래는 -90도, 왼쪽은 좌우 반전합니다. 몸 Collider와 오브젝트 자체는 회전하지 않습니다.")]
    [SerializeField] private bool rotateAttackSprite = true;
    private SpriteRenderer attackBodyVisual;
    private bool attackVisualActive, originalForceRenderingOff;
    private MaterialPropertyBlock attackVisualProperties;

    private void BeginAttackVisual()
    {
        if (!rotateAttackSprite || bodyRenderer == null) return;
        if (attackBodyVisual == null)
        {
            var go = new GameObject("MeleeAttackVisual");
            go.layer = bodyRenderer.gameObject.layer;
            go.transform.SetParent(bodyRenderer.transform, false);
            attackBodyVisual = go.AddComponent<SpriteRenderer>();
            attackVisualProperties = new MaterialPropertyBlock();
        }
        if (!attackVisualActive) originalForceRenderingOff = bodyRenderer.forceRenderingOff;
        attackVisualActive = true;
        bodyRenderer.forceRenderingOff = true;
        SyncAttackVisual();
    }

    private void SyncAttackVisual()
    {
        if (!attackVisualActive) return;
        if (!IsAttacking || !rotateAttackSprite || bodyRenderer == null)
        { EndAttackVisual(); return; }
        // Animator가 원본 Renderer의 Sprite를 갱신한 뒤 동일 프레임을 복사합니다.
        attackBodyVisual.sprite = bodyRenderer.sprite;
        attackBodyVisual.color = bodyRenderer.color;
        attackBodyVisual.sharedMaterial = bodyRenderer.sharedMaterial;
        attackBodyVisual.sortingLayerID = bodyRenderer.sortingLayerID;
        attackBodyVisual.sortingOrder = bodyRenderer.sortingOrder;
        attackBodyVisual.maskInteraction = bodyRenderer.maskInteraction;
        attackBodyVisual.spriteSortPoint = bodyRenderer.spriteSortPoint;
        attackBodyVisual.drawMode = bodyRenderer.drawMode;
        attackBodyVisual.size = bodyRenderer.size;
        attackBodyVisual.flipX = swingDirection.x < 0f;
        attackBodyVisual.flipY = bodyRenderer.flipY;
        attackBodyVisual.enabled = bodyRenderer.enabled && !originalForceRenderingOff;
        bodyRenderer.GetPropertyBlock(attackVisualProperties);
        attackBodyVisual.SetPropertyBlock(attackVisualProperties);
        float angle = swingDirection.y > 0f ? 90f : swingDirection.y < 0f ? -90f : 0f;
        attackBodyVisual.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        attackBodyVisual.gameObject.SetActive(true);
    }

    private void EndAttackVisual()
    {
        if (!attackVisualActive) return;
        if (bodyRenderer != null) bodyRenderer.forceRenderingOff = originalForceRenderingOff;
        if (attackBodyVisual != null) attackBodyVisual.gameObject.SetActive(false);
        attackVisualActive = false;
    }
}
