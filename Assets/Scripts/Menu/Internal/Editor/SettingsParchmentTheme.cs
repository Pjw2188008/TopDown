#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>UI2 양피지 안쪽에 기존 설정 UI를 재배치하는 편집기 도구입니다. 실행 중 배치를 덮어쓰지 않습니다.</summary>
public static class SettingsParchmentTheme
{
    private static readonly Color Ink = new Color(.16f, .10f, .065f, 1);
    private static readonly Color Red = new Color(.43f, .12f, .075f, 1);

    [MenuItem("Tools/Story/Main Menu/Apply UI2 Settings Theme")]
    public static void ApplyToOpenScene()
    {
        var menu = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI2.png");
        if (menu == null || texture == null)
        {
            Debug.LogWarning("MainMenu 씬을 열고 Assets/UI2.png가 있는지 확인하세요.");
            return;
        }
        Undo.RegisterFullObjectHierarchyUndo(menu.menuCanvas.gameObject, "Apply UI2 Settings Theme");
        Apply(menu, texture);
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
    }

    public static void Apply(MainMenuController menu, Texture2D texture)
    {
        Transform frame = menu.settingsPanel.transform.Find("Settings Window");
        Transform confirm = menu.confirmationPanel.transform.Find("Confirmation Window");
        if (frame == null || confirm == null) throw new InvalidOperationException("Settings/Confirmation Window references are missing.");
        menu.settingsPanel.GetComponent<Image>().color = new Color(0, 0, 0, .94f);
        Paper(frame, texture, new Vector2(960, 640));
        Paper(confirm, texture, new Vector2(660, 440));

        foreach (Text text in menu.settingsPanel.GetComponentsInChildren<Text>(true)) text.color = Ink;
        foreach (Text text in menu.confirmationPanel.GetComponentsInChildren<Text>(true)) text.color = Ink;
        foreach (Button button in menu.settingsPanel.GetComponentsInChildren<Button>(true)) StyleButton(button);
        foreach (Button button in menu.confirmationPanel.GetComponentsInChildren<Button>(true)) StyleButton(button);

        var title = frame.Find("설 정");
        if (title != null)
        {
            Place(title, 0, 215, 650, 44);
            title.GetComponent<Text>().fontSize = 30;
            title.GetComponent<Text>().font = GameUIFont.Bold;
            title.GetComponent<Text>().fontStyle = FontStyle.Normal;
        }
        menu.tabSelectionMarks = new GameObject[menu.tabButtons.Length];
        for (int i = 0; i < menu.tabButtons.Length; i++)
        {
            Button tab = menu.tabButtons[i];
            Place(tab.transform, (i - 1) * 250, 160, 220, 44);
            StretchLabel(tab);
            tab.GetComponent<Image>().color = Color.clear;
            if (tab.TryGetComponent(out Outline outline)) outline.enabled = false;
            var mark = Child(tab.transform, "Selected Underline");
            Place(mark, 0, -22, 132, 2);
            var image = mark.GetComponent<Image>() ?? mark.gameObject.AddComponent<Image>();
            image.color = Red; image.raycastTarget = false;
            mark.gameObject.SetActive(i == 0);
            menu.tabSelectionMarks[i] = mark.gameObject;
        }
        foreach (var page in menu.pages) Place(page.transform, 0, -12, 790, 320);
        Place(menu.backButton.transform, 0, -209, 220, 42);
        StretchLabel(menu.backButton);

        Transform display = menu.pages[0].transform;
        MoveNamed(display, "해상도", -260, 91, 175, 42);
        Place(menu.previousResolution.transform, -99, 91, 45, 44);
        Place(menu.resolutionText.transform, 102, 91, 310, 44);
        Place(menu.nextResolution.transform, 303, 91, 45, 44);
        MoveNamed(display, "화면 모드", -260, 16, 175, 42);
        Place(menu.modeButton.transform, 102, 16, 445, 48);
        StretchLabel(menu.modeButton);
        foreach (Text text in display.GetComponentsInChildren<Text>(true))
            if (text.text.StartsWith("해상도를 선택한 뒤")) { Place(text.transform, 0, -65, 750, 86); text.fontSize = 17; }
        Place(menu.applyButton.transform, 0, -151, 260, 42);
        StretchLabel(menu.applyButton);
        StretchLabel(menu.previousResolution); StretchLabel(menu.nextResolution);

        Transform controls = menu.pages[1].transform;
        int itemIndex = 0;
        foreach (Text text in controls.GetComponentsInChildren<Text>(true))
        {
            var rect = text.rectTransform;
            if (text.text.StartsWith("키 변경 기능"))
            {
                Place(rect, 0, -157, 760, 28); text.fontSize = 15;
                continue;
            }
            // 기존 10행의 순서/설명은 유지하며 행 간격과 열 폭만 줄입니다.
            int row = itemIndex++ / 2;
            bool key = rect.anchoredPosition.x < 0;
            Place(rect, key ? -277 : 104, 126 - row * 28, key ? 180 : 568, 28);
            text.fontSize = 17;
            text.color = key ? Red : Ink;
        }

        Transform audio = menu.pages[2].transform;
        MoveNamed(audio, "전체 음량", -250, 87, 205, 42);
        Place(menu.volumeText.transform, 270, 87, 140, 42);
        Place(menu.volumeSlider.transform, 0, 10, 650, 40);
        var track = menu.volumeSlider.transform.Find("Track");
        Place(track, 0, 0, 650, 8); track.GetComponent<Image>().color = new Color(.27f, .16f, .08f, .3f);
        menu.volumeSlider.fillRect.GetComponent<Image>().color = Red;
        menu.volumeSlider.targetGraphic.color = Ink;
        menu.volumeSlider.handleRect.sizeDelta = new Vector2(18, -12);
        Place(menu.muteToggle.transform, 0, -72, 200, 40);
        menu.muteToggle.targetGraphic.color = new Color(.3f, .18f, .08f, .28f);
        menu.muteToggle.graphic.color = Red;
        foreach (Text text in audio.GetComponentsInChildren<Text>(true))
            if (text.text.StartsWith("음량은 즉시")) { Place(text.transform, 0, -143, 745, 65); text.fontSize = 18; }

        Place(menu.confirmationText.transform, 0, 38, 525, 110); menu.confirmationText.fontSize = 23;
        Place(menu.keepButton.transform, -130, -87, 195, 44);
        Place(menu.revertButton.transform, 130, -87, 195, 44);
        StretchLabel(menu.keepButton); StretchLabel(menu.revertButton);
        var heading = Child(confirm, "Confirmation Heading");
        Place(heading, 0, 136, 500, 42);
        var headingText = heading.GetComponent<Text>() ?? heading.gameObject.AddComponent<Text>();
        headingText.font = menu.confirmationText.font; headingText.fontSize = 25; headingText.color = Ink;
        headingText.text = "화면 설정 확인"; headingText.alignment = TextAnchor.MiddleCenter; headingText.raycastTarget = false;
    }

    private static void Paper(Transform frame, Texture2D texture, Vector2 size)
    {
        Place(frame, 0, 0, size.x, size.y);
        frame.GetComponent<Image>().color = Color.clear;
        foreach (Outline outline in frame.GetComponents<Outline>()) outline.enabled = false;
        RectTransform paper = Child(frame, "Parchment Background (UI2)");
        paper.SetAsFirstSibling(); paper.anchorMin = Vector2.zero; paper.anchorMax = Vector2.one;
        paper.offsetMin = paper.offsetMax = Vector2.zero;
        var raw = paper.GetComponent<RawImage>() ?? paper.gameObject.AddComponent<RawImage>();
        raw.texture = texture; raw.color = Color.white; raw.raycastTarget = false;
    }

    private static void StyleButton(Button button)
    {
        button.GetComponent<Image>().color = Color.white;
        var colors = button.colors;
        colors.normalColor = new Color(.33f, .18f, .075f, .08f);
        colors.highlightedColor = new Color(.33f, .18f, .075f, .18f);
        colors.selectedColor = colors.highlightedColor; colors.pressedColor = new Color(.43f, .12f, .075f, .3f);
        button.colors = colors;
        if (button.TryGetComponent(out Outline outline)) { outline.enabled = true; outline.effectColor = new Color(.3f, .16f, .06f, .55f); }
        StretchLabel(button);
    }
    private static void StretchLabel(Button button)
    {
        var text = button.GetComponentInChildren<Text>(true);
        var rect = text.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(8, 2); rect.offsetMax = new Vector2(-8, -2);
        text.color = Ink; text.fontSize = 22;
    }
    private static RectTransform Child(Transform parent, string name)
    {
        var existing = parent.Find(name) as RectTransform;
        if (existing != null) return existing;
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); return rect;
    }
    private static void MoveNamed(Transform root, string name, float x, float y, float w, float h)
    {
        var child = root.Find(name); if (child != null) Place(child, x, y, w, h);
    }
    private static void Place(Transform target, float x, float y, float w, float h)
    {
        var rect = (RectTransform)target;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(w, h);
    }
}
#endif
