#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>편집 가능한 첫 화면 씬을 생성합니다. 런타임에는 실행되지 않습니다.</summary>
public static class MainMenuSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private static readonly Color Ink = new Color(.085f, .072f, .059f, .95f);
    private static readonly Color Paper = new Color(.96f, .9f, .78f);
    private static Font font;

    [MenuItem("Tools/Story/Main Menu/Create Menu Scene (New Only)")]
    public static void CreateMenu()
    {
        if (File.Exists(ScenePath))
        {
            Debug.LogWarning("MainMenu 씬이 이미 있습니다. 덮어쓰지 않습니다. 직접 열어서 수정하세요.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Build();
    }

    // 격리된 검증 프로젝트에서도 동일한 씬 생성기를 사용합니다.
    public static Scene Build()
    {
        if (File.Exists(ScenePath)) throw new IOException("Existing MainMenu scene is protected.");
        font = GameUIFont.Regular;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Menu Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Ink;
        camera.orthographic = true;
        camera.transform.position = new Vector3(0, 0, -10);
        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

        var root = new GameObject("Main Menu Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var controller = root.AddComponent<MainMenuController>();
        controller.menuCanvas = canvas;

        var background = Rect("Background (UI.png)", root.transform, Vector2.zero, new Vector2(1280, 720));
        var image = background.gameObject.AddComponent<RawImage>();
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI.png");
        image.texture = texture;
        image.raycastTarget = false;
        var fit = background.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = texture != null ? (float)texture.width / texture.height : 16f / 9f;

        var main = Full("Main Buttons", root.transform);
        controller.mainPanel = main.gameObject;
        controller.startButton = Button("게임 시작", main, new Vector2(0, -40), new Vector2(300, 62));
        controller.settingsButton = Button("설정", main, new Vector2(0, -116), new Vector2(300, 62));
        controller.quitButton = Button("게임 종료", main, new Vector2(0, -192), new Vector2(300, 62));
        ApplyTextButtonStyle(controller.startButton);
        ApplyTextButtonStyle(controller.settingsButton);
        ApplyTextButtonStyle(controller.quitButton);
        var subtitle = Rect("Subtitle Backdrop", main, new Vector2(0, -246), new Vector2(300, 30));
        subtitle.gameObject.AddComponent<Image>().color = new Color(.085f, .072f, .059f, .75f);
        Label("첫 번째 이야기 · 튜토리얼", subtitle, Vector2.zero, new Vector2(300, 30), 17);
        controller.statusText = Label("", root.transform, new Vector2(0, -310), new Vector2(1000, 50), 18);

        var settings = Full("Settings Overlay", root.transform);
        settings.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .72f);
        settings.gameObject.AddComponent<CanvasGroup>();
        controller.settingsPanel = settings.gameObject;
        var frame = Panel("Settings Window", settings, Vector2.zero, new Vector2(900, 620));
        Label("설 정", frame, new Vector2(0, 265), new Vector2(800, 44), 32);
        controller.tabButtons = new[] {
            Button("화면", frame, new Vector2(-265, 203), new Vector2(245, 46)),
            Button("조작법", frame, new Vector2(0, 203), new Vector2(245, 46)),
            Button("소리", frame, new Vector2(265, 203), new Vector2(245, 46)) };
        var display = Rect("Display Page", frame, new Vector2(0, -18), new Vector2(800, 370));
        var controls = Rect("Controls Page", frame, new Vector2(0, -18), new Vector2(800, 370));
        var audio = Rect("Audio Page", frame, new Vector2(0, -18), new Vector2(800, 370));
        controller.pages = new[] { display.gameObject, controls.gameObject, audio.gameObject };
        Label("해상도", display, new Vector2(-270, 115), new Vector2(180, 44), 24);
        controller.previousResolution = Button("◀", display, new Vector2(-100, 115), new Vector2(50, 48));
        controller.resolutionText = Label("1920 × 1080", display, new Vector2(105, 115), new Vector2(320, 48), 24);
        controller.nextResolution = Button("▶", display, new Vector2(310, 115), new Vector2(50, 48));
        Label("화면 모드", display, new Vector2(-270, 32), new Vector2(180, 44), 24);
        controller.modeButton = Button("전체 화면 (테두리 없음)", display, new Vector2(105, 32), new Vector2(460, 52));
        controller.modeText = controller.modeButton.GetComponentInChildren<Text>();
        Label("해상도를 선택한 뒤 적용을 누르세요.\n15초 안에 유지하지 않으면 이전 설정으로 돌아갑니다.\n※ 에디터에서는 미리보기만 하며, 실제 화면 변경은 빌드에서 적용됩니다.",
            display, new Vector2(0, -74), new Vector2(770, 95), 18);
        controller.applyButton = Button("화면 설정 적용", display, new Vector2(0, -160), new Vector2(260, 50));

        string[] keys = { "W / A / S / D", "Space", "마우스 이동", "좌클릭", "우클릭", "E", "1 / 2", "F", "B", "마우스 휠" };
        string[] actions = {
            "이동 (대각선 포함)", "짧게 누르기: 대시  /  꾹 누르기: 달리기", "공격 방향 / 오류·대상 조준",
            "일반: 공격  /  편집: Cut 또는 준비된 Paste", "누르고 있기: 가드  /  피격 직전: 패링", "편집 모드 켜기 / 끄기",
            "전투: 오류 사용  /  편집: Paste 준비 (좌클릭 적용)", "물체 상호작용 / 놓기", "오류 도감 열기 / 닫기", "보관함이 가득 찼을 때 교체 대상 선택" };
        for (int i = 0; i < keys.Length; i++)
        {
            float y = 157 - i * 32;
            var key = Label(keys[i], controls, new Vector2(-290, y), new Vector2(190, 30), 18);
            key.color = new Color(.88f, .71f, .44f);
            Label(actions[i], controls, new Vector2(105, y), new Vector2(600, 30), 18, TextAnchor.MiddleLeft);
        }
        Label("키 변경 기능이 아닌 현재 조작 안내입니다.  |  Esc: 설정 닫기", controls,
            new Vector2(0, -185), new Vector2(800, 30), 16);

        Label("전체 음량", audio, new Vector2(-260, 110), new Vector2(210, 44), 24);
        controller.volumeText = Label("100%", audio, new Vector2(290, 110), new Vector2(160, 44), 24);
        var sliderRect = Rect("Master Volume Slider", audio, new Vector2(0, 30), new Vector2(670, 42));
        var slider = sliderRect.gameObject.AddComponent<Slider>();
        var track = Rect("Track", sliderRect, Vector2.zero, new Vector2(670, 10));
        track.gameObject.AddComponent<Image>().color = new Color(.3f, .25f, .19f);
        var fillArea = Full("Fill Area", sliderRect); fillArea.offsetMin = new Vector2(0, 16); fillArea.offsetMax = new Vector2(0, -16);
        var fill = Full("Fill", fillArea); fill.gameObject.AddComponent<Image>().color = new Color(.76f, .58f, .32f);
        var handleArea = Full("Handle Area", sliderRect); handleArea.offsetMin = new Vector2(10, 0); handleArea.offsetMax = new Vector2(-10, 0);
        var handle = Rect("Handle", handleArea, Vector2.zero, new Vector2(22, 34));
        Image handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = Paper;
        slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = handleImage;
        slider.minValue = 0; slider.maxValue = 1; slider.value = 1;
        controller.volumeSlider = slider;
        var toggleRect = Rect("Mute Toggle", audio, new Vector2(0, -65), new Vector2(200, 42));
        Toggle toggle = toggleRect.gameObject.AddComponent<Toggle>();
        var box = Rect("Box", toggleRect, new Vector2(-68, 0), new Vector2(32, 32));
        Image boxImage = box.gameObject.AddComponent<Image>(); boxImage.color = new Color(.34f, .29f, .22f);
        var check = Rect("Check", box, Vector2.zero, new Vector2(20, 20));
        Image checkImage = check.gameObject.AddComponent<Image>(); checkImage.color = Paper;
        toggle.targetGraphic = boxImage; toggle.graphic = checkImage;
        Label("음소거", toggleRect, new Vector2(35, 0), new Vector2(130, 42), 23);
        controller.muteToggle = toggle;
        Label("음량은 즉시 적용되며 게임 시작 후에도 유지됩니다.\n음악과 효과음을 포함한 전체 음량을 조절합니다.", audio,
            new Vector2(0, -150), new Vector2(770, 70), 19);
        controller.backButton = Button("돌아가기", frame, new Vector2(0, -266), new Vector2(245, 50));

        var confirmation = Full("Display Confirmation", root.transform);
        confirmation.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .85f);
        controller.confirmationPanel = confirmation.gameObject;
        var confirmFrame = Panel("Confirmation Window", confirmation, Vector2.zero, new Vector2(680, 270));
        controller.confirmationText = Label("이 화면 설정을 유지할까요?", confirmFrame, new Vector2(0, 45), new Vector2(630, 110), 24);
        controller.keepButton = Button("유지", confirmFrame, new Vector2(-150, -75), new Vector2(240, 52));
        controller.revertButton = Button("되돌리기", confirmFrame, new Vector2(150, -75), new Vector2(240, 52));
        confirmation.gameObject.SetActive(false);
        settings.gameObject.SetActive(false);
        controls.gameObject.SetActive(false);
        audio.gameObject.SetActive(false);
        Texture2D settingsArt = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI2.png");
        if (settingsArt != null) SettingsParchmentTheme.Apply(controller, settingsArt);
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("MAIN_MENU_SCENE_CREATED " + ScenePath);
        return scene;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }

    private static RectTransform Full(string name, Transform parent)
    {
        var rect = Rect(name, parent, Vector2.zero, Vector2.zero);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static RectTransform Panel(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = Rect(name, parent, position, size);
        rect.gameObject.AddComponent<Image>().color = Ink;
        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(.67f, .51f, .29f, .9f); outline.effectDistance = new Vector2(1, -1);
        return rect;
    }

    private static Text Label(string text, Transform parent, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        var label = Rect(string.IsNullOrEmpty(text) ? "Status" : text.Split('\n')[0], parent, position, size).gameObject.AddComponent<Text>();
        label.font = font; label.fontSize = fontSize; label.text = text;
        label.color = Paper; label.alignment = alignment; label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        return label;
    }

    private static Button Button(string caption, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = Panel(caption, parent, position, size);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
        var colors = button.colors; colors.highlightedColor = new Color(1.5f, 1.35f, 1.15f); colors.pressedColor = new Color(.8f, .6f, .35f);
        colors.selectedColor = new Color(1.5f, 1.35f, 1.15f); button.colors = colors;
        Label(caption, rect, Vector2.zero, size - new Vector2(12, 4), 24);
        return button;
    }

    /// <summary>버튼 위치/크기/문구를 보존한 채 시작 메뉴용 투명 스타일만 설정합니다.</summary>
    public static void ApplyTextButtonStyle(Button button)
    {
        if (button.TryGetComponent<MenuButtonFeedback>(out _)) return;
        var image = button.GetComponent<Image>();
        if (image != null) { image.color = Color.clear; image.raycastTarget = true; }
        foreach (var outline in button.GetComponents<Outline>()) outline.enabled = false;
        button.transition = Selectable.Transition.None;
        var label = button.GetComponentInChildren<Text>(true);
        var shadow = label.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, .9f);
        shadow.effectDistance = new Vector2(1.5f, -2f);
        var feedback = button.gameObject.AddComponent<MenuButtonFeedback>();
        feedback.label = label;
        label.color = feedback.normalColor;
        var ornaments = Full("Selection Ornaments", button.transform);
        var group = ornaments.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0; group.interactable = false; group.blocksRaycasts = false;
        feedback.ornaments = group;
        for (int side = -1; side <= 1; side += 2)
        {
            var tip = Rect(side < 0 ? "Left Diamond" : "Right Diamond", ornaments,
                new Vector2(side * 122, 0), new Vector2(7, 7));
            tip.localRotation = Quaternion.Euler(0, 0, 45);
            var diamond = tip.gameObject.AddComponent<Image>(); diamond.color = feedback.selectedColor; diamond.raycastTarget = false;
            var line = Rect(side < 0 ? "Left Line" : "Right Line", ornaments,
                new Vector2(side * 101, 0), new Vector2(22, 1.5f));
            var stroke = line.gameObject.AddComponent<Image>(); stroke.color = feedback.selectedColor; stroke.raycastTarget = false;
        }
    }
}
#endif
