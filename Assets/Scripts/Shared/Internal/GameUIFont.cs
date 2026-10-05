using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Canvas와 임시 OnGUI UI가 같은 폰트를 사용하게 합니다. 직접 부착하지 않습니다.</summary>
public static class GameUIFont
{
    private static GameUIFontSettings settings;
    private static GUISkin gameSkin;
    private static GUISkin sourceSkin;
    public static Font Regular => Settings != null ? Settings.regular : null;
    public static Font Bold => Settings != null && Settings.bold != null ? Settings.bold : Regular;
    private static GameUIFontSettings Settings => settings != null ? settings : (settings = Resources.Load<GameUIFontSettings>("GameUIFont"));

    public static void Apply(Text label, Font regularOverride = null)
    {
        if (label == null) return;
        bool bold = label.fontStyle == FontStyle.Bold || (Bold != null && label.font == Bold);
        Font font = bold ? Bold : (regularOverride != null ? regularOverride : Regular);
        if (font == null) return;
        label.font = font;
        label.fontStyle = FontStyle.Normal; // 실제 Bold 파일을 사용하므로 합성 굵기는 중복 적용하지 않습니다.
    }

    /// <summary>OnGUI 안에서 using으로 사용합니다. 종료 시 다른 UI의 원래 스킨을 복원합니다.</summary>
    public static SkinScope UseIMGUI() => new SkinScope(false);

    public readonly struct SkinScope : IDisposable
    {
        private readonly GUISkin previous;
        internal SkinScope(bool unused)
        {
            previous = GUI.skin;
            if (Regular == null) return;
            if (gameSkin == null || sourceSkin != previous)
            {
                if (gameSkin != null) UnityEngine.Object.Destroy(gameSkin);
                sourceSkin = previous;
                gameSkin = UnityEngine.Object.Instantiate(previous);
                gameSkin.hideFlags = HideFlags.HideAndDontSave;
                gameSkin.font = Regular;
                foreach (GUIStyle style in gameSkin)
                {
                    style.font = style.fontStyle == FontStyle.Bold ? Bold : Regular;
                    style.fontStyle = FontStyle.Normal;
                }
            }
            GUI.skin = gameSkin;
        }
        public void Dispose() { if (previous != null) GUI.skin = previous; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()
    {
        if (gameSkin != null) UnityEngine.Object.Destroy(gameSkin);
        gameSkin = null;
        sourceSkin = null;
        settings = null;
    }
}
