using System.Collections.Generic;
using UnityEngine;

/// <summary>EnemyController의 공격 예고/이펙트/기즈모 부분입니다. partial 구현이므로 직접 부착하지 않습니다.</summary>
public partial class EnemyController
{
    private Sprite GetFallbackSprite()
    {
        if (fallbackSprite != null) return fallbackSprite;
        fallbackTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        fallbackTexture.SetPixel(0, 0, Color.white); fallbackTexture.Apply();
        fallbackSprite = Sprite.Create(fallbackTexture, new Rect(0,0,1,1), Vector2.one*.5f, 1f);
        return fallbackSprite;
    }

    private void ShowEffect(bool warning)
    {
        if (effectObject == null) { effectObject = new GameObject("EnemyMeleeAttackEffect"); effectRenderer = effectObject.AddComponent<SpriteRenderer>(); }
        Sprite sprite = warning ? GetFallbackSprite() : attackEffectSprite;
        if (sprite == null && target != null) sprite = target.AttackEffectVisual;
        if (sprite == null) sprite = GetFallbackSprite();
        effectRenderer.sprite = sprite;
        effectRenderer.color = warning ? new Color(1f,.65f,.1f,.25f) : new Color(1f,.35f,.25f,.8f);
        if (bodyRenderer != null) effectRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
        effectRenderer.sortingOrder = (bodyRenderer != null ? bodyRenderer.sortingOrder : 0) + 2;
        Vector2 size = sprite.bounds.size;
        Vector3 scale = new Vector3(HitSize.x / Mathf.Max(.0001f,size.x), HitSize.y / Mathf.Max(.0001f,size.y), 1f);
        Quaternion rotation = Quaternion.Euler(0,0,SwingAngle);
        effectObject.transform.localScale = scale; effectObject.transform.rotation = rotation;
        effectObject.transform.position = new Vector3(swingCenter.x,swingCenter.y,transform.position.z)
            - rotation * Vector3.Scale(sprite.bounds.center, scale);
        effectObject.SetActive(true);
    }

    private void UpdateEffect(float dt)
    {
        if (windingUp) return;
        effectRemaining = Mathf.Max(0f, effectRemaining-dt);
        if (effectRemaining <= 0f && effectObject != null) effectObject.SetActive(false);
    }

    private void CancelAttack()
    {
        windingUp = false; effectRemaining = 0f;
        if (effectObject != null) effectObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireCube(transform.position, (Vector3)patrolAreaSize);
        Gizmos.color = Color.red; Gizmos.DrawWireCube(transform.position, (Vector3)attackAreaSize);
        Matrix4x4 old = Gizmos.matrix;
        Vector2 center = Application.isPlaying && (windingUp || effectRemaining > 0f) ? swingCenter
            : (Vector2)transform.position + swingDirection * attackReach;
        Gizmos.matrix = Matrix4x4.TRS((Vector3)center, Quaternion.Euler(0,0,SwingAngle), Vector3.one);
        Gizmos.color = Color.cyan; Gizmos.DrawWireCube(Vector3.zero, (Vector3)HitSize); Gizmos.matrix = old;
    }
}
