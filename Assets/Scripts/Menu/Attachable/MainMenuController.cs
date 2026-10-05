using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>첫 화면의 시작/설정/종료, 해상도 변경 확인, 조작 안내, 전체 음량을 담당합니다.</summary>
public sealed class MainMenuController : MonoBehaviour
{
    [Header("게임 시작")]
    [Tooltip("Build Settings에 등록한 튜토리얼 씬 경로. 기존 REDACT 씬을 그대로 엽니다.")]
    public string tutorialScene = "Assets/Scenes/REDACT.unity";
    [Tooltip("씬 전환 중에만 로딩 우선순위를 높여 로딩에 더 많은 처리 시간을 줍니다. 완료/실패/종료 시 이전 설정으로 복원됩니다.")]
    public bool prioritizeSceneLoading = true;
    [Tooltip("본문 폰트를 직접 연결할 수 있습니다. 비우면 Resources/GameUIFont의 PF스타더스트를 사용합니다.")]
    public Font koreanFont;
    [Header("메인 화면 / 설정 창")]
    [Tooltip("직접 위치와 크기를 조정할 수 있는 메뉴 Canvas입니다.")] public Canvas menuCanvas;
    [Tooltip("메인 버튼 묶음입니다.")] public GameObject mainPanel;
    [Tooltip("설정 전체 패널입니다.")] public GameObject settingsPanel;
    [Tooltip("화면 / 조작법 / 소리 탭 내용 순서입니다.")] public GameObject[] pages;
    [Tooltip("화면 / 조작법 / 소리 탭 버튼 순서입니다.")] public Button[] tabButtons;
    [Tooltip("선택된 설정 탭 아래 표시할 장식입니다. 탭 버튼과 같은 순서로 연결합니다.")]
    public GameObject[] tabSelectionMarks;
    [Tooltip("튜토리얼 시작 버튼입니다.")] public Button startButton;
    [Tooltip("설정 열기 버튼입니다.")] public Button settingsButton;
    [Tooltip("게임 종료 버튼입니다.")] public Button quitButton;
    [Tooltip("설정을 닫고 메인 메뉴로 돌아갑니다.")] public Button backButton;
    [Tooltip("로딩 또는 시작 실패 사유를 표시합니다.")] public Text statusText;
    [Header("화면 설정")]
    [Tooltip("선택 해상도 표시입니다.")] public Text resolutionText;
    [Tooltip("선택 창 모드 표시입니다.")] public Text modeText;
    [Tooltip("이전 해상도 버튼입니다.")] public Button previousResolution;
    [Tooltip("다음 해상도 버튼입니다.")] public Button nextResolution;
    [Tooltip("전체 화면 / 창 모드 전환 버튼입니다.")] public Button modeButton;
    [Tooltip("선택한 해상도와 창 모드를 적용합니다.")] public Button applyButton;
    [Tooltip("적용 후 15초 안에 유지 여부를 묻는 패널입니다.")] public GameObject confirmationPanel;
    [Tooltip("화면 변경 확인 및 남은 시간을 표시합니다.")] public Text confirmationText;
    [Tooltip("변경한 화면 설정을 저장합니다.")] public Button keepButton;
    [Tooltip("변경 전 화면 설정으로 돌아갑니다.")] public Button revertButton;
    [Header("소리")]
    [Tooltip("전체 음량 0~1. 모든 AudioSource의 최종 출력에 적용합니다.")] public Slider volumeSlider;
    [Tooltip("전체 음소거 토글입니다.")] public Toggle muteToggle;
    [Tooltip("음량 백분율을 표시합니다.")] public Text volumeText;

    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
    private int resolutionIndex;
    private FullScreenMode selectedMode;
    private int oldWidth, oldHeight;
    private FullScreenMode oldMode;
    private float confirmDeadline;
    private bool pendingDisplay, loading;
    private Coroutine mainActionRoutine;
    private bool mainActionPending;
    private MenuSceneLoadOperation sceneLoad;
    private int displayedLoadingPercent = -1;

    private void Awake()
    {
        ApplyFont();
        startButton.onClick.AddListener(() => QueueMainAction(startButton, StartGame));
        settingsButton.onClick.AddListener(() => QueueMainAction(settingsButton, OpenSettings));
        quitButton.onClick.AddListener(() => QueueMainAction(quitButton, QuitGame));
        backButton.onClick.AddListener(CloseSettings);
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int page = i;
            tabButtons[i].onClick.AddListener(() => ShowPage(page));
        }
        previousResolution.onClick.AddListener(() => CycleResolution(-1));
        nextResolution.onClick.AddListener(() => CycleResolution(1));
        modeButton.onClick.AddListener(ToggleMode);
        applyButton.onClick.AddListener(ApplyDisplay);
        keepButton.onClick.AddListener(KeepDisplay);
        revertButton.onClick.AddListener(RevertDisplay);
        volumeSlider.SetValueWithoutNotify(MenuPreferences.Volume);
        muteToggle.SetIsOnWithoutNotify(MenuPreferences.Muted);
        volumeSlider.onValueChanged.AddListener(_ => ChangeAudio());
        muteToggle.onValueChanged.AddListener(_ => ChangeAudio());
        MenuPreferences.ApplyAudio();
        UpdateVolumeLabel();
        confirmationPanel.SetActive(false);
        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);
        statusText.text = string.Empty;
    }

    private void Start() => Select(startButton);

    private void QueueMainAction(Button source, Action action)
    {
        if (loading || mainActionPending || !source.IsInteractable() || !mainPanel.activeInHierarchy) return;
        mainActionPending = true;
        float delay = source.TryGetComponent(out MenuButtonFeedback feedback) ? feedback.PlayClick() : 0f;
        mainActionRoutine = StartCoroutine(PerformMainAction(action, delay));
    }

    private IEnumerator PerformMainAction(Action action, float delay)
    {
        // 클릭 표시를 본 뒤 전환합니다. 연속 클릭으로 여러 동작이 예약되지 않습니다.
        yield return new WaitForSecondsRealtime(delay);
        mainActionRoutine = null;
        mainActionPending = false;
        action();
    }

    private void Update()
    {
        if (loading && sceneLoad != null)
        {
            // Unity의 0.9 이후는 씬 활성화 단계입니다. 완료 전에는 100%로 표시하지 않습니다.
            int percent = Mathf.Clamp(Mathf.FloorToInt(sceneLoad.Progress / .9f * 99f), 0, 99);
            if (percent != displayedLoadingPercent)
            {
                displayedLoadingPercent = percent;
                statusText.text = percent >= 99 ? "이야기 시작 준비 중…" : $"이야기 속으로 들어가는 중… {percent}%";
            }
        }
        if (pendingDisplay)
        {
            float remaining = confirmDeadline - Time.unscaledTime;
            if (remaining <= 0f) RevertDisplay();
            else confirmationText.text = $"이 화면 설정을 유지할까요?\n{Mathf.CeilToInt(remaining)}초 후 이전 설정으로 돌아갑니다.";
        }
        if (!loading && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (pendingDisplay) RevertDisplay();
            else if (settingsPanel.activeSelf) CloseSettings();
        }
    }

    private void ApplyFont()
    {
        foreach (Text label in menuCanvas.GetComponentsInChildren<Text>(true))
            GameUIFont.Apply(label, koreanFont);
    }

    public void OpenSettings()
    {
        if (loading) return;
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
        RefreshResolutions();
        ShowPage(0);
        Select(tabButtons[0]);
    }

    public void CloseSettings()
    {
        if (pendingDisplay) RevertDisplay();
        PlayerPrefs.Save();
        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);
        Select(settingsButton);
    }

    public void ShowPage(int index)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == index);
            if (tabSelectionMarks != null && i < tabSelectionMarks.Length && tabSelectionMarks[i] != null)
                tabSelectionMarks[i].SetActive(i == index);
            // 색상만 변경합니다. 크기/위치/글꼴은 사용자가 편집한 값을 유지합니다.
            ColorBlock colors = tabButtons[i].colors;
            colors.normalColor = i == index ? new Color(.65f, .5f, .3f) : Color.white;
            tabButtons[i].colors = colors;
        }
    }

    private void RefreshResolutions()
    {
        resolutions.Clear();
        foreach (Resolution r in Screen.resolutions) AddResolution(r.width, r.height);
        AddResolution(1280, 720);
        AddResolution(1600, 900);
        AddResolution(1920, 1080);
        AddResolution(Screen.width, Screen.height);
        resolutions.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        resolutionIndex = resolutions.IndexOf(new Vector2Int(Screen.width, Screen.height));
        selectedMode = Screen.fullScreenMode == FullScreenMode.Windowed ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
        RefreshDisplayLabels();
    }

    private void AddResolution(int width, int height)
    {
        Vector2Int value = new Vector2Int(width, height);
        if (width > 0 && height > 0 && !resolutions.Contains(value)) resolutions.Add(value);
    }

    public void CycleResolution(int direction)
    {
        if (pendingDisplay || resolutions.Count == 0) return;
        resolutionIndex = (resolutionIndex + direction % resolutions.Count + resolutions.Count) % resolutions.Count;
        RefreshDisplayLabels();
    }

    public void ToggleMode()
    {
        if (pendingDisplay) return;
        selectedMode = selectedMode == FullScreenMode.Windowed ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        RefreshDisplayLabels();
    }

    private void RefreshDisplayLabels()
    {
        Vector2Int size = resolutions[Mathf.Clamp(resolutionIndex, 0, resolutions.Count - 1)];
        resolutionText.text = $"{size.x} × {size.y}";
        modeText.text = selectedMode == FullScreenMode.Windowed ? "창 모드" : "전체 화면 (테두리 없음)";
    }

    public void ApplyDisplay()
    {
        if (pendingDisplay || resolutions.Count == 0) return;
        oldWidth = Screen.width;
        oldHeight = Screen.height;
        oldMode = Screen.fullScreenMode;
        Vector2Int size = resolutions[resolutionIndex];
        SetDisplay(size.x, size.y, selectedMode);
        pendingDisplay = true;
        confirmDeadline = Time.unscaledTime + 15f;
        SetSettingsInteractable(false);
        confirmationText.text = "이 화면 설정을 유지할까요?\n15초 후 이전 설정으로 돌아갑니다.";
        confirmationPanel.SetActive(true);
        Select(keepButton);
    }

    private static void SetDisplay(int width, int height, FullScreenMode mode)
    {
#if !UNITY_EDITOR
        Screen.SetResolution(width, height, mode);
#endif
    }

    public void KeepDisplay()
    {
        if (!pendingDisplay) return;
        Vector2Int size = resolutions[resolutionIndex];
        MenuPreferences.SaveDisplay(size.x, size.y, selectedMode);
        pendingDisplay = false;
        confirmationPanel.SetActive(false);
        SetSettingsInteractable(true);
        Select(applyButton);
    }

    public void RevertDisplay()
    {
        if (!pendingDisplay) return;
        SetDisplay(oldWidth, oldHeight, oldMode);
        pendingDisplay = false;
        confirmationPanel.SetActive(false);
        SetSettingsInteractable(true);
        RefreshResolutions();
        Select(applyButton);
    }

    private void ChangeAudio()
    {
        MenuPreferences.SaveAudio(volumeSlider.value, muteToggle.isOn);
        UpdateVolumeLabel();
    }

    private void SetSettingsInteractable(bool value)
    {
        if (settingsPanel.TryGetComponent(out CanvasGroup group)) group.interactable = value;
    }

    private void UpdateVolumeLabel() => volumeText.text = $"{Mathf.RoundToInt(volumeSlider.value * 100)}%";

    public void StartGame()
    {
        if (loading || MenuSceneLoadOperation.IsBusy) return;
        if (!Application.CanStreamedLevelBeLoaded(tutorialScene))
        {
            statusText.text = "튜토리얼 씬이 Build Profiles의 Scene List에 등록되어 있는지 확인하세요.";
            return;
        }
        loading = true;
        startButton.interactable = settingsButton.interactable = quitButton.interactable = false;
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        statusText.text = "이야기 속으로 들어가는 중…";
        try { sceneLoad = MenuSceneLoadOperation.Begin(tutorialScene, prioritizeSceneLoading); }
        catch (Exception ex)
        {
            loading = false;
            startButton.interactable = settingsButton.interactable = quitButton.interactable = true;
            statusText.text = "튜토리얼을 열지 못했습니다. Console을 확인하세요.";
            Debug.LogException(ex, this);
        }
    }

    public void QuitGame()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void Select(Selectable control)
    {
        if (EventSystem.current != null && control != null && control.gameObject.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(control.gameObject);
    }

    private void OnDisable()
    {
        if (mainActionRoutine != null) StopCoroutine(mainActionRoutine);
        mainActionRoutine = null;
        mainActionPending = false;
        if (pendingDisplay) RevertDisplay();
        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
    }
}
