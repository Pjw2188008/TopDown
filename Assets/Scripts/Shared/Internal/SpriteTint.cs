using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 원래 몸 색상과 일시적인 피격 색상을 분리합니다. 직접 부착하지 않습니다.
/// 오류 효과는 GetColor/SetColor로 기본 색을 다루며, 피격 연출이 끝나면 최신 기본 색으로 복구합니다.
/// </summary>
public static class SpriteTint
{
    private sealed class Flash
    {
        public object owner;
        public Color baseColor;
        public Color appliedColor;
    }

    private static readonly Dictionary<SpriteRenderer, Flash> flashes = new Dictionary<SpriteRenderer, Flash>();

    public static Color GetColor(SpriteRenderer renderer)
    {
        if (renderer == null) return Color.white;
        if (!flashes.TryGetValue(renderer, out var flash)) return renderer.color;
        CaptureExternalColor(renderer, flash);
        return flash.baseColor;
    }

    public static void SetColor(SpriteRenderer renderer, Color color)
    {
        if (renderer == null) return;
        if (flashes.TryGetValue(renderer, out var flash))
        {
            flash.baseColor = color;
            flash.appliedColor.a = color.a;
            renderer.color = flash.appliedColor;
        }
        else renderer.color = color;
    }

    public static void ShowFlash(SpriteRenderer renderer, object owner, Color color)
    {
        if (renderer == null) return;
        if (!flashes.TryGetValue(renderer, out var flash))
        {
            flash = new Flash { baseColor = renderer.color };
            flashes.Add(renderer, flash);
        }
        else CaptureExternalColor(renderer, flash);
        flash.owner = owner;
        color.a = flash.baseColor.a; // 피격으로 원래 투명도를 바꾸지 않습니다.
        flash.appliedColor = color;
        renderer.color = color;
    }

    public static void ClearFlash(SpriteRenderer renderer, object owner)
    {
        // Unity에서 파괴된 Renderer도 관리 객체 키로는 남으므로 항목을 제거합니다.
        if (ReferenceEquals(renderer, null) || !flashes.TryGetValue(renderer, out var flash) || !ReferenceEquals(flash.owner, owner)) return;
        if (renderer != null)
        {
            CaptureExternalColor(renderer, flash);
            renderer.color = flash.baseColor;
        }
        flashes.Remove(renderer);
    }

    private static void CaptureExternalColor(SpriteRenderer renderer, Flash flash)
    {
        // Animator 등 외부에서 직접 바꾼 색도 가능한 한 최신 복구 색으로 반영합니다.
        if (!renderer.color.Equals(flash.appliedColor)) flash.baseColor = renderer.color;
    }
}
