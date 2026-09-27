using UnityEngine;

/// <summary>메뉴에서 저장한 전체 음량/음소거와 화면 설정을 다음 실행에도 적용합니다.</summary>
public static class MenuPreferences
{
    private const string Prefix = "Redact.Settings.";
    public static float Volume => Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Volume", 1f));
    public static bool Muted => PlayerPrefs.GetInt(Prefix + "Muted", 0) != 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        ApplyAudio();
#if !UNITY_EDITOR
        if (PlayerPrefs.HasKey(Prefix + "Width"))
            Screen.SetResolution(Mathf.Max(640, PlayerPrefs.GetInt(Prefix + "Width")),
                Mathf.Max(480, PlayerPrefs.GetInt(Prefix + "Height")),
                PlayerPrefs.GetInt(Prefix + "Fullscreen", 1) != 0 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
#endif
    }

    public static void ApplyAudio() => AudioListener.volume = Muted ? 0f : Volume;

    public static void SaveAudio(float volume, bool muted)
    {
        PlayerPrefs.SetFloat(Prefix + "Volume", Mathf.Clamp01(volume));
        PlayerPrefs.SetInt(Prefix + "Muted", muted ? 1 : 0);
        ApplyAudio();
    }

    public static void SaveDisplay(int width, int height, FullScreenMode mode)
    {
        PlayerPrefs.SetInt(Prefix + "Width", width);
        PlayerPrefs.SetInt(Prefix + "Height", height);
        PlayerPrefs.SetInt(Prefix + "Fullscreen", mode == FullScreenMode.Windowed ? 0 : 1);
        PlayerPrefs.Save();
    }
}
