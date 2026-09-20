using UnityEngine;

/// <summary>
/// 좌측 상단 상태 게이지와 이름 기반 오류 슬롯을 그리는 임시 HUD입니다.
/// PlayerMove partial로 별도 부착/이미지/Canvas 설정 없이 동작합니다. 게임 수치와 입력은 변경하지 않습니다.
/// </summary>
public partial class PlayerMove
{
    private GUIStyle hudLabelStyle, hudValueStyle, hudSlotStyle, hudCaptionStyle, hudKeyStyle;
    private static readonly Color HudPanelColor = new Color(0.055f, 0.07f, 0.095f, 0.94f);
    private static readonly Color HudBorderColor = new Color(0.27f, 0.32f, 0.4f, 1f);
    private static readonly Color HudSelectedColor = new Color(0.2f, 0.85f, 1f, 1f);

    private void DrawPlayerHud()
    {
        if (!showPlayerHud || !Application.isPlaying) return;
        EnsureHudStyles();
        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        float fit = Mathf.Min(Screen.width / 640f, Screen.height / 520f);
        float scale = Mathf.Max(0.01f, Mathf.Min(Mathf.Clamp(playerHudScale, 0.5f, 2f), fit));
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        GUI.color = Color.white;
        try
        {
            float y = 20f;
            DrawHudGauge(y, "HP", currentHealth, maxHealth, new Color(0.92f, 0.28f, 0.32f));
            y += 56f;
            if (showDashStamina)
            {
                DrawHudGauge(y, "대쉬 스태미나", currentDashStamina, maxDashStamina, new Color(0.95f, 0.77f, 0.25f));
                y += 56f;
            }
            if (showGuardGauge)
            {
                DrawHudGauge(y, "가드", currentGuardGauge, maxGuardGauge, new Color(0.3f, 0.65f, 1f));
                y += 56f;
            }
            DrawStoredErrorSlots(y + 10f);
        }
        finally { GUI.matrix = previousMatrix; GUI.color = previousColor; }
    }

    private void EnsureHudStyles()
    {
        if (hudLabelStyle != null) return;
        hudLabelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleLeft };
        hudLabelStyle.normal.textColor = Color.white;
        hudValueStyle = new GUIStyle(hudLabelStyle) { alignment = TextAnchor.MiddleRight };
        hudSlotStyle = new GUIStyle(hudLabelStyle)
        { alignment = TextAnchor.MiddleCenter, fontSize = 17, fontStyle = FontStyle.Bold, wordWrap = true };
        hudCaptionStyle = new GUIStyle(hudLabelStyle)
        { fontSize = 12, alignment = TextAnchor.UpperLeft, wordWrap = true };
        hudCaptionStyle.normal.textColor = new Color(0.76f, 0.82f, 0.89f);
        hudKeyStyle = new GUIStyle(hudLabelStyle) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
    }

    private static void DrawHudRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private static void DrawHudPanel(Rect rect, Color border)
    {
        DrawHudRect(rect, border);
        DrawHudRect(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), HudPanelColor);
    }

    private void DrawHudGauge(float y, string label, float value, float maximum, Color color)
    {
        maximum = Mathf.Max(1f, maximum);
        value = Mathf.Clamp(value, 0f, maximum);
        DrawHudPanel(new Rect(20f, y, 288f, 50f), HudBorderColor);
        GUI.Label(new Rect(32f, y + 5f, 152f, 23f), label, hudLabelStyle);
        GUI.Label(new Rect(184f, y + 5f, 112f, 23f), $"{value:0.#} / {maximum:0.#}", hudValueStyle);
        Rect bar = new Rect(32f, y + 32f, 264f, 9f);
        DrawHudRect(bar, new Color(0.14f, 0.17f, 0.22f));
        if (value > 0f) DrawHudRect(new Rect(bar.x, bar.y, bar.width * (value / maximum), bar.height), color);
    }

    private void DrawStoredErrorSlots(float y)
    {
        GUI.Label(new Rect(20f, y, 288f, 22f), "오류 보관함", hudLabelStyle);
        float slotY = y + 26f;
        for (int slot = 0; slot < MaxStoredErrors; slot++)
        {
            StoredErrorType error = storedErrors.GetSlot(slot);
            bool selected = error != StoredErrorType.None && isEditMode
                && environmentPaste.IsArmed && environmentPaste.SelectedError == error;
            bool replacing = isReplacingStoredError && error != StoredErrorType.None
                && (GetPendingCutReplacementCount() >= storedErrors.Count
                    || GetStoredErrorTypeAtIndex(selectedReplacementIndex) == error);
            Rect rect = new Rect(20f + slot * 100f, slotY, 88f, 88f);
            DrawHudPanel(rect, replacing ? new Color(1f, 0.64f, 0.2f) : selected ? HudSelectedColor : HudBorderColor);
            DrawHudRect(new Rect(rect.x + 7f, rect.y + 7f, 20f, 20f), HudBorderColor);
            GUI.Label(new Rect(rect.x + 7f, rect.y + 7f, 20f, 20f), (slot + 1).ToString(), hudKeyStyle);
            string name = error == StoredErrorType.None ? "빈 슬롯" : GetStoredErrorDisplayName(error).Trim('[', ']');
            hudSlotStyle.normal.textColor = error == StoredErrorType.None ? new Color(0.48f, 0.55f, 0.63f) : Color.white;
            GUI.Label(new Rect(rect.x + 5f, rect.y + 29f, 78f, 32f), name, hudSlotStyle);
            if (selected || replacing) GUI.Label(new Rect(rect.x + 5f, rect.y + 64f, 78f, 18f),
                replacing ? "교체 대상" : "Paste 준비", hudKeyStyle);
        }
        string hint = isEditMode ? "1 / 2 · 오류 선택    좌클릭 · Paste\n같은 번호 · 취소" : "1 / 2 · 전투 오류 사용";
        if (isReplacingStoredError) hint = "휠 · 교체 슬롯 선택    좌클릭 · 확정";
        else if (IsAnyCombatErrorActive()) hint = GetActiveCombatErrorDisplayName() + " 전투 효과 유지 중";
        else if (Time.unscaledTime < dashFeedbackUntil) hint = "대쉬 스태미나가 부족합니다.";
        else if (guardRequiresRelease) hint = "가드 해제 · 우클릭을 놓고 다시 누르세요.";
        else if (isGuarding) hint += "\n가드 중";
        GUI.Label(new Rect(20f, slotY + 98f, 300f, 44f), hint, hudCaptionStyle);
        if (isReplacingStoredError)
        {
            Rect box = new Rect(20f, slotY + 148f, 308f, 94f);
            DrawHudPanel(box, new Color(1f, 0.64f, 0.2f));
            GUI.Label(new Rect(box.x + 10f, box.y + 8f, 288f, 22f),
                "획득 " + GetPendingCutDisplayName(), hudLabelStyle);
            GUI.Label(new Rect(box.x + 10f, box.y + 34f, 288f, 54f),
                "주황색 슬롯의 오류를 교체합니다.\n휠 · 교체 대상 변경  /  좌클릭 · 확정\nE · 취소 및 편집 모드 종료", hudCaptionStyle);
        }
    }
}
