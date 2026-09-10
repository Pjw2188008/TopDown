using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 주변 활성 오류 발견, 영구 도감 저장, 클릭형 상세 UI를 담당하는 PlayerMove partial입니다.
/// 별도 부착/씬 UI 연결 없이 동작합니다. 발견은 Cut 획득/오류 노출과 독립적입니다.
/// </summary>
public partial class PlayerMove
{
    private const string ErrorCodexSavePrefix = "Story.ErrorCodex.v1.";
    private readonly ErrorCodex errorCodex = new ErrorCodex();
    private bool isErrorCodexOpen;
    private bool closeErrorCodexRequested;
    private float timeScaleBeforeCodex;
    private float fixedDeltaBeforeCodex;
    private float nextErrorDiscoveryScan;
    private float discoveryNoticeUntil;
    private string discoveryNotice = "";
    private StoredErrorType selectedCodexError;
    private Vector2 codexDetailScroll;
    private GUIStyle codexBodyStyle;
    private GUIStyle codexTitleStyle;
    // 시작 시 결정해 테스트 도중 Inspector 값을 바꾸어도 임시 기록이 기존 저장에 섞이지 않게 합니다.
    private bool usePersistentErrorDiscoveries;

    private void LoadErrorDiscoveries()
    {
        errorCodex.Clear();
        selectedCodexError = StoredErrorType.None;
        codexDetailScroll = Vector2.zero;
        discoveryNotice = "";
        discoveryNoticeUntil = 0f;
        nextErrorDiscoveryScan = 0f;
        usePersistentErrorDiscoveries = persistErrorDiscoveries
            && !(Application.isEditor && startWithEmptyErrorCodexInEditor);
        if (!usePersistentErrorDiscoveries) return;
        foreach (StoredErrorType type in ErrorCodex.Entries)
            if (PlayerPrefs.GetInt(ErrorCodexSavePrefix + (int)type, 0) == 1)
                errorCodex.TryDiscover(type);
    }

    private void UpdateErrorDiscovery()
    {
        if (errorCodex.Count == ErrorCodex.Entries.Count || Time.timeScale <= 0f
            || Time.unscaledTime < nextErrorDiscoveryScan) return;
        nextErrorDiscoveryScan = Time.unscaledTime + 0.2f;

        // 데모 규모의 오류 컴포넌트만 0.2초마다 확인합니다. 비활성 오브젝트는 검색에서 제외됩니다.
        // Collider/레이어 설정 없이 동작하며 런타임 생성된 오류 원본도 자동으로 인식합니다.
        string newlyDiscovered = "";
        ScanErrorSources<GiantErrorEffect>(ref newlyDiscovered);
        ScanErrorSources<ReflectionErrorEffect>(ref newlyDiscovered);
        ScanErrorSources<AccelerationErrorEffect>(ref newlyDiscovered);
        if (newlyDiscovered.Length == 0) return;
        if (usePersistentErrorDiscoveries) PlayerPrefs.Save();
        discoveryNotice = "이상현상 발견: " + newlyDiscovered + "\n도감에 등록했습니다. [" + errorCodexKey + "] 도감 열기";
        discoveryNoticeUntil = Time.unscaledTime + 4f;
        Debug.Log(discoveryNotice, this);
    }

    private void ScanErrorSources<T>(ref string newlyDiscovered) where T : MonoBehaviour, IErrorSource
    {
        foreach (T source in FindObjectsByType<T>(FindObjectsSortMode.None))
        {
            if (!source.isActiveAndEnabled || !source.IsActive || errorCodex.IsDiscovered(source.ErrorType)) continue;
            Vector3 difference = source.transform.position - transform.position;
            if (!ErrorCodex.IsWithinRange(difference.x, difference.y, errorDiscoveryRadius)) continue;
            if (!errorCodex.TryDiscover(source.ErrorType)) continue;
            if (usePersistentErrorDiscoveries) PlayerPrefs.SetInt(ErrorCodexSavePrefix + (int)source.ErrorType, 1);
            if (newlyDiscovered.Length > 0) newlyDiscovered += " / ";
            newlyDiscovered += ErrorRules.DisplayName(source.ErrorType);
            if (selectedCodexError == StoredErrorType.None) selectedCodexError = source.ErrorType;
        }
    }

    /// <summary>모달을 연/닫은 프레임도 입력을 소비하여 UI 클릭의 전투 전달을 막습니다.</summary>
    private bool HandleErrorCodexInput()
    {
        bool toggle = Keyboard.current != null && Keyboard.current[errorCodexKey].wasPressedThisFrame;
        bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        if (isErrorCodexOpen)
        {
            if (toggle || escape || closeErrorCodexRequested) CloseErrorCodex();
            return true;
        }
        if (!toggle) return false;
        OpenErrorCodex();
        return true;
    }

    private void OpenErrorCodex()
    {
        timeScaleBeforeCodex = Time.timeScale;
        fixedDeltaBeforeCodex = Time.fixedDeltaTime;
        isErrorCodexOpen = true;
        closeErrorCodexRequested = false;
        environmentPaste.Cancel();
        isSelectingStoredError = false;
        if (isReplacingStoredError) CancelStoredErrorReplacement(null);
        EndGuard();
        Time.timeScale = 0f;
        // fixedDeltaTime을 0으로 만들지 않습니다. 닫을 때 기존 편집 모드 속도를 그대로 복원합니다.
        if (!errorCodex.IsDiscovered(selectedCodexError))
        {
            selectedCodexError = StoredErrorType.None;
            foreach (StoredErrorType type in ErrorCodex.Entries)
                if (errorCodex.IsDiscovered(type)) { selectedCodexError = type; break; }
        }
    }

    private void CloseErrorCodex()
    {
        if (!isErrorCodexOpen) return;
        isErrorCodexOpen = false;
        closeErrorCodexRequested = false;
        Time.timeScale = timeScaleBeforeCodex;
        Time.fixedDeltaTime = fixedDeltaBeforeCodex;
    }

    /// <summary>간단한 프로토타입 UI. 항목 클릭은 설명 선택만 하며 실제 오류를 지급하지 않습니다.</summary>
    private bool DrawErrorCodex()
    {
        if (!Application.isPlaying) return false;
        if (!isErrorCodexOpen)
        {
            GUI.Box(new Rect(Mathf.Max(0f, Screen.width - 230f), 20f, 210f, 32f),
                $"[{errorCodexKey}] 오류 도감  {errorCodex.Count}/{ErrorCodex.Entries.Count}");
            if (Time.unscaledTime < discoveryNoticeUntil)
                GUI.Box(new Rect(Mathf.Max(0f, (Screen.width - 480f) / 2f), 60f, 480f, 58f), discoveryNotice);
            return false;
        }

        // 기준 해상도 좌표계를 축소해 작은 Game 뷰에서도 목록/닫기 버튼이 화면 밖으로 나가지 않게 합니다.
        Matrix4x4 oldMatrix = GUI.matrix;
        int oldDepth = GUI.depth;
        GUI.depth = -100;
        float scale = Mathf.Max(0.01f, Mathf.Min(Screen.width / 900f, Screen.height / 620f));
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 900f * scale) / 2f,
            (Screen.height - 620f * scale) / 2f, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
        if (codexBodyStyle == null)
        {
            codexBodyStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 17 };
            codexTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
        }
        GUI.Box(new Rect(10f, 10f, 880f, 600f), GUIContent.none);
        GUI.Label(new Rect(35f, 28f, 650f, 36f), $"이상현상 도감  ·  {errorCodex.Count}/{ErrorCodex.Entries.Count}", codexTitleStyle);
        if (GUI.Button(new Rect(740f, 28f, 125f, 34f), "닫기 [Esc]")) closeErrorCodexRequested = true;
        GUI.Label(new Rect(35f, 70f, 825f, 45f),
            "발견 기록은 보관함과 별개입니다. 항목을 클릭해 설명을 확인하세요.\n도감에서는 오류를 꺼낼 수 없습니다. 열려 있는 동안 게임이 일시 정지됩니다.");
        for (int i = 0; i < ErrorCodex.Entries.Count; i++)
        {
            StoredErrorType type = ErrorCodex.Entries[i];
            bool known = errorCodex.IsDiscovered(type);
            bool oldEnabled = GUI.enabled;
            GUI.enabled = oldEnabled && known;
            string title = known ? ErrorRules.DisplayName(type) : "??? · 미발견";
            if (type == selectedCodexError) title = "▶ " + title;
            if (GUI.Button(new Rect(35f, 130f + i * 60f, 200f, 48f), title))
            {
                selectedCodexError = type;
                codexDetailScroll = Vector2.zero;
            }
            GUI.enabled = oldEnabled;
        }
        GUI.Box(new Rect(255f, 125f, 610f, 430f), GUIContent.none);
        GUILayout.BeginArea(new Rect(273f, 140f, 572f, 395f));
        codexDetailScroll = GUILayout.BeginScrollView(codexDetailScroll);
        if (errorCodex.IsDiscovered(selectedCodexError))
        {
            GUILayout.Label(ErrorRules.DisplayName(selectedCodexError), codexTitleStyle);
            GUILayout.Space(10f);
            GUILayout.Label(ErrorCodex.Description(selectedCodexError), codexBodyStyle);
            GUILayout.Space(18f);
            GUILayout.Label("Paste 호환 대상", codexTitleStyle);
            GUILayout.Label(ErrorCodex.Compatibility(selectedCodexError), codexBodyStyle);
        }
        else GUILayout.Label("아직 발견한 오류가 없습니다.\n이상현상이 발생한 대상 가까이 다가가 보세요.", codexBodyStyle);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
        GUI.Label(new Rect(35f, 570f, 810f, 24f), $"[{errorCodexKey}] 또는 [Esc] 닫기  ·  발견해도 자동 Cut/보관되지 않습니다.");
        GUI.matrix = oldMatrix;
        GUI.depth = oldDepth;
        return true;
    }

    private void DrawErrorDiscoveryGizmo()
    {
        if (!showErrorDiscoveryGizmo) return;
        Color oldColor = Gizmos.color;
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = new Color(0.3f, 1f, 0.5f, 1f);
        float radius = Mathf.Max(0f, errorDiscoveryRadius);
        Vector3 center = transform.position;
        Vector3 previous = center + Vector3.right * radius;
        for (int i = 1; i <= 64; i++)
        {
            float angle = i * Mathf.PI * 2f / 64f;
            Vector3 next = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
        Gizmos.color = oldColor;
        Gizmos.matrix = oldMatrix;
    }

    [ContextMenu("오류 도감/발견 기록 초기화 (저장 기록 포함)")]
    private void ResetErrorDiscoveries()
    {
        // 다른 PlayerPrefs(게임 설정/세이브)는 삭제하지 않습니다. 테스트용 수동 실행입니다.
        foreach (StoredErrorType type in ErrorCodex.Entries)
            PlayerPrefs.DeleteKey(ErrorCodexSavePrefix + (int)type);
        PlayerPrefs.Save();
        errorCodex.Clear();
        selectedCodexError = StoredErrorType.None;
        discoveryNoticeUntil = 0f;
        nextErrorDiscoveryScan = 0f;
        Debug.Log("오류 도감 발견 기록을 초기화했습니다. 범위 내 활성 오류는 다음 검사에서 다시 발견됩니다.", this);
    }
}
