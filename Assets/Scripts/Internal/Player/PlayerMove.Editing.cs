using UnityEngine;
using UnityEngine.UI;

/// <summary>PlayerMove의 편집 모드·화면 UI 구현 부분입니다. 별도 컴포넌트가 아니므로 직접 부착하지 않습니다.</summary>
public partial class PlayerMove
{
    private void ToggleEditMode()
    {
        isSelectingStoredError = false;
        isEditMode = !isEditMode;

        if (!isEditMode && isReplacingStoredError)
        {
            CancelStoredErrorReplacement("편집 모드가 종료되어 오류 교체를 취소했습니다.");
        }

        Time.timeScale = isEditMode ? editTimeScale : 1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        if (isEditMode)
        {
            Debug.Log("편집 모드 활성화 - 시간 흐름이 느려집니다.");
        }
        else
        {
            Debug.Log("편집 모드 종료");
        }
    }

    private void CreateEditModeOverlay()
    {
        GameObject canvasObject = new GameObject("EditModeOverlayCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject overlayObject = new GameObject("EditModeOverlay");
        overlayObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rectTransform = overlayObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        editModeOverlay = overlayObject.AddComponent<Image>();
        editModeOverlay.color = new Color(0f, 0f, 0f, 0f);
        editModeOverlay.raycastTarget = false;
    }

    private void UpdateEditModeVisual()
    {
        if (editModeOverlay == null)
        {
            return;
        }

        Color targetColor = isEditMode ? new Color(0f, 0f, 0f, editModeDarkness) : new Color(0f, 0f, 0f, 0f);
        editModeOverlay.color = Color.Lerp(editModeOverlay.color, targetColor, Time.unscaledDeltaTime * editModeFadeSpeed);
    }

    private void OnGUI()
    {
        if (isReplacingStoredError)
        {
            string replacementText = $"새 오류 {GetStoredErrorDisplayName(pendingReplacementErrorType)}\n"
                + "교체할 오류 선택\n";
            int replacementCandidateCount = GetStoredErrorCount();

            for (int index = 0; index < replacementCandidateCount; index++)
            {
                string marker = index == selectedReplacementIndex ? ">> " : "    ";
                replacementText += marker + GetStoredErrorDisplayName(GetStoredErrorTypeAtIndex(index)) + "\n";
            }

            replacementText += "마우스 휠: 변경  |  좌클릭: 교체 확정";
            GUI.Box(new Rect(20f, 20f, 330f, 132f), replacementText);
            return;
        }

        if (!isSelectingStoredError || GetStoredErrorCount() < 2)
        {
            return;
        }

        string selectionText = "오류 선택  |  Q를 놓으면 확정\n";
        int storedErrorCount = GetStoredErrorCount();

        for (int index = 0; index < storedErrorCount; index++)
        {
            string marker = index == selectedStoredErrorIndex ? "▶ " : "   ";
            selectionText += marker + GetStoredErrorDisplayName(GetStoredErrorTypeAtIndex(index)) + "\n";
        }

        selectionText += "마우스 휠로 변경";
        GUI.Box(new Rect(20f, 20f, 260f, 92f), selectionText);
    }
}
