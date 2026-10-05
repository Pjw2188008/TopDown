using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Inspector에서 수정할 수 있는 시작 튜토리얼과 도착 구역을 만드는 메뉴입니다.</summary>
public static class TutorialSetup
{
    [MenuItem("GameObject/Story/Movement Tutorial", false, 12)]
    private static void Create(MenuCommand command)
    {
        var root = new GameObject("Movement Tutorial");
        GameObjectUtility.SetParentAndAlign(root, command.context as GameObject);
        var tutorial = root.AddComponent<TutorialSequence>();
        var player = Object.FindFirstObjectByType<PlayerMove>();
        var goal = new GameObject("Arrival Area - Move Here");
        goal.transform.SetParent(root.transform, false);
        goal.transform.position = (player != null ? player.transform.position : root.transform.position) + Vector3.up * 4f;
        goal.layer = 2; // Ignore Raycast: 일반 투사체가 튜토리얼 구역에 맞지 않게 합니다.
        var area = goal.AddComponent<BoxCollider2D>(); area.isTrigger = true; area.size = new Vector2(3f, 2f);
        var data = new SerializedObject(tutorial);
        data.FindProperty("player").objectReferenceValue = player;
        data.FindProperty("steps").GetArrayElementAtIndex(0).FindPropertyRelative("arrivalArea").objectReferenceValue = area;
        data.ApplyModifiedPropertiesWithoutUndo();
        Undo.RegisterCreatedObjectUndo(root, "Create Movement Tutorial");
        BuildUI(tutorial);
        Selection.activeGameObject = root;
    }

    public static void BuildUI(TutorialSequence tutorial)
    {
        if (tutorial == null || Application.isPlaying) return;
        var data = new SerializedObject(tutorial);
        var existing = data.FindProperty("instructionPanel").objectReferenceValue as GameObject;
        if (existing != null) { Selection.activeGameObject = existing; return; }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Create editable tutorial UI");
        var canvasObject = new GameObject("Tutorial Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(tutorial.transform, false);
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 50;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var panel = new GameObject("Instruction Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        var rect = panel.GetComponent<RectTransform>();
        bool right = data.FindProperty("pinPanelToTopRight").boolValue;
        rect.anchorMin = rect.anchorMax = rect.pivot = right ? Vector2.one : new Vector2(0, 1);
        var position = data.FindProperty("panelPosition").vector2Value;
        rect.anchoredPosition = right ? new Vector2(-20, -20) : new Vector2(position.x, -position.y);
        var size = data.FindProperty("panelSize").vector2Value;
        rect.sizeDelta = new Vector2(Mathf.Max(160, size.x), Mathf.Max(80, size.y));
        var background = panel.GetComponent<Image>(); background.color = new Color(.05f, .07f, .12f, .88f); background.raycastTarget = false;
        Font font = data.FindProperty("instructionFont").objectReferenceValue as Font;
        if (font == null) font = GameUIFont.Regular;
        var title = MakeText("Title", panel.transform, font, 16, TextAnchor.UpperLeft);
        title.font = GameUIFont.Bold; title.fontStyle = FontStyle.Normal; title.text = "튜토리얼 1";
        var titleRect = title.rectTransform; titleRect.anchorMin = new Vector2(0, 1); titleRect.anchorMax = Vector2.one;
        titleRect.pivot = new Vector2(.5f, 1); titleRect.anchoredPosition = new Vector2(0, -10); titleRect.sizeDelta = new Vector2(-28, 26);
        var body = MakeText("Message", panel.transform, font, Mathf.Max(10, data.FindProperty("fontSize").intValue), TextAnchor.UpperLeft);
        body.rectTransform.anchorMin = Vector2.zero; body.rectTransform.anchorMax = Vector2.one;
        body.rectTransform.offsetMin = new Vector2(14, 12); body.rectTransform.offsetMax = new Vector2(-14, -42);
        var steps = data.FindProperty("steps"); body.text = steps.arraySize > 0 ? steps.GetArrayElementAtIndex(0).FindPropertyRelative("message").stringValue : "튜토리얼 안내";
        var marker = MakeText("Destination Marker", canvasObject.transform, font, 18, TextAnchor.MiddleCenter);
        marker.text = "목표 지점 ↓"; marker.rectTransform.anchorMin = marker.rectTransform.anchorMax = new Vector2(.5f,.5f);
        marker.rectTransform.sizeDelta = new Vector2(180, 36); marker.rectTransform.anchoredPosition = new Vector2(0, 80);
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create editable tutorial UI");
        data.FindProperty("instructionPanel").objectReferenceValue = panel;
        data.FindProperty("messageText").objectReferenceValue = body;
        data.FindProperty("titleText").objectReferenceValue = title;
        data.FindProperty("destinationMarker").objectReferenceValue = marker.rectTransform;
        data.ApplyModifiedProperties();
        Undo.CollapseUndoOperations(group); EditorUtility.SetDirty(tutorial); Selection.activeGameObject = panel;
    }

    public static void AddCutTutorialSteps(TutorialSequence tutorial)
    {
        if (tutorial == null || Application.isPlaying) return;
        var data = new SerializedObject(tutorial);
        var steps = data.FindProperty("steps");
        for (int i = 0; i < steps.arraySize; i++)
            if (steps.GetArrayElementAtIndex(i).FindPropertyRelative("completionCondition").intValue == (int)TutorialSequence.CompletionCondition.CutSucceeded)
            { Debug.Log("이미 Cut 성공 단계가 있습니다. Steps에서 문구와 순서를 수정하세요.", tutorial); return; }
        // 처음 생성한 기본 대기 문구만 교체합니다. 사용자가 작성한 단계/이벤트는 삭제하지 않습니다.
        if (steps.arraySize > 0)
        {
            var last = steps.GetArrayElementAtIndex(steps.arraySize - 1);
            if (last.FindPropertyRelative("message").stringValue == "이동 연습 완료!\n다음 튜토리얼 안내를 여기에 설정하세요."
                && last.FindPropertyRelative("arrivalArea").objectReferenceValue == null
                && last.FindPropertyRelative("completionCondition").intValue == 0
                && last.FindPropertyRelative("minimumDisplaySeconds").floatValue == 0f
                && last.FindPropertyRelative("onEntered.m_PersistentCalls.m_Calls").arraySize == 0)
                steps.arraySize--;
        }
        AddStep(steps, TutorialSequence.CompletionCondition.EditModeEnabled, 1f,
            "E 키를 눌러 편집 모드를 켜세요.\n편집 모드에서는 오류를 Cut할 수 있어요.");
        AddStep(steps, TutorialSequence.CompletionCondition.CutSucceeded, .5f,
            "{target}에 가까이 가서 조준하세요.\n편집 모드에서 좌클릭해 Cut하세요.");
        AddStep(steps, TutorialSequence.CompletionCondition.Confirm, 2f,
            "{error} 오류가 보관함에 저장됐어요.\nCut하면 원본의 오류는 사라집니다.\n잠시 후 자동으로 진행합니다.");
        Undo.SetCurrentGroupName("Add edit mode and Cut tutorial");
        data.ApplyModifiedProperties(); EditorUtility.SetDirty(tutorial);
    }

    public static void AddGiantPasteTutorialSteps(TutorialSequence tutorial)
    {
        if (tutorial == null || Application.isPlaying) return;
        var data = new SerializedObject(tutorial);
        var steps = data.FindProperty("steps");
        for (int i = 0; i < steps.arraySize; i++)
        {
            var step = steps.GetArrayElementAtIndex(i);
            if (step.FindPropertyRelative("completionCondition").intValue == (int)TutorialSequence.CompletionCondition.EnvironmentPasteSucceeded
                && step.FindPropertyRelative("pasteError").intValue == (int)StoredErrorType.Giant)
            { Debug.Log("이미 거대화 Paste 단계가 있습니다. Steps의 대상과 문구를 수정하세요.", tutorial); return; }
        }
        AddStep(steps, TutorialSequence.CompletionCondition.EnvironmentPasteSucceeded, .5f,
            "편집 모드(E)에서 진행하세요.\n{error} 슬롯 번호(1/2)를 누르고\n{target}을 조준해 좌클릭하세요.");
        AddStep(steps, TutorialSequence.CompletionCondition.Confirm, 2f,
            "거대화가 적용되어 물체가 커져요!\nPaste한 오류는 보관함에서 사라집니다.\n잠시 후 자동으로 진행합니다.");
        Undo.SetCurrentGroupName("Add Giant Paste tutorial");
        data.ApplyModifiedProperties(); EditorUtility.SetDirty(tutorial);
    }

    public static void AddCombatTutorialSteps(TutorialSequence tutorial)
    {
        if (tutorial == null || Application.isPlaying) return;
        var data = new SerializedObject(tutorial);
        var steps = data.FindProperty("steps");
        for (int i = 0; i < steps.arraySize; i++)
            if (steps.GetArrayElementAtIndex(i).FindPropertyRelative("completionCondition").intValue == (int)TutorialSequence.CompletionCondition.TrainingAttackHit)
            { Debug.Log("이미 공격 연습 단계가 있습니다. 기존 Combat Practice 구역과 허수아비를 조정하세요.", tutorial); return; }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Add attack and parry tutorial");
        var root = new GameObject("Combat Practice"); root.transform.SetParent(tutorial.transform, false);
        var player = data.FindProperty("player").objectReferenceValue as PlayerMove;
        if (player == null) player = Object.FindFirstObjectByType<PlayerMove>();
        Vector3 origin = player != null ? player.transform.position : tutorial.transform.position;
        var areaObject = new GameObject("Combat Arrival Area"); areaObject.transform.SetParent(root.transform, false);
        areaObject.transform.position = origin + Vector3.up * 4f; areaObject.layer = 2;
        var area = areaObject.AddComponent<BoxCollider2D>(); area.isTrigger = true; area.size = new Vector2(3, 2);
        var dummyObject = new GameObject("Training Dummy"); dummyObject.SetActive(false);
        dummyObject.transform.SetParent(root.transform, false); dummyObject.transform.position = origin + Vector3.up * 7f;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0) dummyObject.layer = enemyLayer;
        else Debug.LogWarning("Enemy 레이어가 없습니다. 허수아비 Layer를 플레이어 Enemy Layer 마스크에 포함하세요.", tutorial);
        var dummy = dummyObject.AddComponent<TutorialTrainingDummy>();
        dummyObject.GetComponent<SpriteRenderer>().sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        dummyObject.GetComponent<SpriteRenderer>().color = new Color(.7f, .5f, .25f);
        var collider = dummyObject.GetComponent<BoxCollider2D>(); collider.isTrigger = true; collider.size = Vector2.one;
        Undo.RegisterCreatedObjectUndo(root, "Create combat practice objects");
        int start = steps.arraySize;
        AddStep(steps, TutorialSequence.CompletionCondition.ArrivalArea, 0f,
            "공격과 패링을 연습해 볼까요?\n표시된 연습 구역으로 이동하세요.");
        var arrival = steps.GetArrayElementAtIndex(start);
        arrival.FindPropertyRelative("arrivalArea").objectReferenceValue = area;
        arrival.FindPropertyRelative("combatDummy").objectReferenceValue = dummy;
        AddStep(steps, TutorialSequence.CompletionCondition.TrainingAttackHit, .5f,
            "편집 모드라면 E로 해제하세요.\n허수아비 가까이서 조준하고 좌클릭!\n공격을 {count}회 맞혀 보세요.");
        steps.GetArrayElementAtIndex(start + 1).FindPropertyRelative("combatDummy").objectReferenceValue = dummy;
        AddStep(steps, TutorialSequence.CompletionCondition.TrainingProjectileParried, .5f,
            "투사체가 닿기 직전에 우클릭!\n놓았다 다시 눌러 타이밍을 맞추고\n패링을 {count}회 성공하세요.");
        steps.GetArrayElementAtIndex(start + 2).FindPropertyRelative("combatDummy").objectReferenceValue = dummy;
        AddStep(steps, TutorialSequence.CompletionCondition.Confirm, 2f,
            "첨삭 성공! 투사체를 튕겨냈어요.\n우클릭 유지: 가드 / 맞기 직전: 패링\n잠시 후 자동으로 진행합니다.");
        data.ApplyModifiedProperties(); EditorUtility.SetDirty(tutorial);
        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = areaObject;
    }

    private static void AddStep(SerializedProperty steps, TutorialSequence.CompletionCondition condition, float seconds, string message)
    {
        int index = steps.arraySize; steps.arraySize++;
        var item = steps.GetArrayElementAtIndex(index);
        item.FindPropertyRelative("completionCondition").intValue = (int)condition;
        item.FindPropertyRelative("minimumDisplaySeconds").floatValue = seconds;
        item.FindPropertyRelative("message").stringValue = message;
        item.FindPropertyRelative("arrivalArea").objectReferenceValue = null;
        item.FindPropertyRelative("cutTarget").objectReferenceValue = null;
        item.FindPropertyRelative("cutTargetDisplayName").stringValue = string.Empty;
        item.FindPropertyRelative("cutTargetMarkerAnchor").objectReferenceValue = null;
        item.FindPropertyRelative("pasteError").intValue = (int)StoredErrorType.Giant;
        item.FindPropertyRelative("pasteTarget").objectReferenceValue = null;
        item.FindPropertyRelative("pasteTargetDisplayName").stringValue = string.Empty;
        item.FindPropertyRelative("pasteTargetMarkerAnchor").objectReferenceValue = null;
        item.FindPropertyRelative("combatDummy").objectReferenceValue = null;
        item.FindPropertyRelative("requiredCombatSuccesses").intValue = 1;
        item.FindPropertyRelative("onEntered.m_PersistentCalls.m_Calls").ClearArray();
    }

    private static Text MakeText(string name, Transform parent, Font font, int size, TextAnchor alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>(); text.font = font; text.fontSize = size; text.alignment = alignment;
        text.color = Color.white; text.raycastTarget = false; text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }
}

[CustomEditor(typeof(TutorialSequence))]
public sealed class TutorialSequenceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        using (new EditorGUI.DisabledScope(Application.isPlaying))
            if (GUILayout.Button("편집 모드 / Cut 안내 3단계 추가")) TutorialSetup.AddCutTutorialSteps((TutorialSequence)target);
        using (new EditorGUI.DisabledScope(Application.isPlaying))
            if (GUILayout.Button("거대화 Paste 안내 2단계 추가")) TutorialSetup.AddGiantPasteTutorialSteps((TutorialSequence)target);
        using (new EditorGUI.DisabledScope(Application.isPlaying))
            if (GUILayout.Button("공격 / 패링 연습 구역 + 4단계 추가")) TutorialSetup.AddCombatTutorialSteps((TutorialSequence)target);
        EditorGUILayout.HelpBox("공격/패링 버튼은 Combat Practice 아래 진입 구역과 비활성 허수아비를 만들고 4단계를 붙입니다. 구역/허수아비 위치를 맵에 맞게 옮기세요. 활성 단계의 구역 도착 후 등장하며, 공격 적중과 해당 허수아비 투사체의 실제 패링으로 진행합니다. Required Combat Successes로 횟수를 조절합니다.", MessageType.Info);
        EditorGUILayout.HelpBox("거대화 Paste 안내는 기존 단계 뒤에 추가됩니다. EnvironmentPasteSucceeded 단계의 Paste Target에 작은 상자를 연결하고, 상자에는 Collider2D와 PasteTarget(Object)을 붙이세요. Paste Error는 Giant입니다. 다른 대상/오류, 전투 사용, 선택/취소/실패는 지정 대상 완료로 인정하지 않습니다. 대상 미연결 시에는 모든 환경 대상의 거대화 Paste를 인정합니다.", MessageType.Info);
        EditorGUILayout.HelpBox("성공 조건을 달성하면 자동으로 다음 단계로 진행합니다. 완료 안내(Auto Advance, 기존 Confirm)는 Minimum Display Seconds 후 자동 진행하며 0으로 설정하면 즉시 넘어갑니다. Enter는 필요 없습니다. Manual은 외부 호출 대기를 유지합니다.", MessageType.Info);
        EditorGUILayout.HelpBox("CutSucceeded 단계의 Cut Target에 오류 원본 오브젝트를 연결하면 해당 대상 위에 표식을 표시하고 그 대상(또는 자식)을 Cut해야 진행합니다. Cut Target Display Name에는 '거대한 상자'처럼 플레이어용 이름을 적으세요. 미연결이면 기존처럼 아무 원본이나 인정합니다.", MessageType.Info);
        EditorGUILayout.HelpBox("패널 위치/크기는 RectTransform, 배경은 Image, 글꼴/색상은 Text에서 직접 편집합니다. 단계 문구는 Steps의 Message를 사용합니다. 실행 중 패널 배치를 덮어쓰지 않습니다.", MessageType.Info);
        var panel = serializedObject.FindProperty("instructionPanel").objectReferenceValue as GameObject;
        if (panel == null)
        {
            EditorGUILayout.HelpBox("Canvas UI가 연결되지 않았습니다. 아래 버튼으로 생성하거나 직접 만든 UI를 연결하세요. 기존 IMGUI 안내는 더 이상 표시하지 않습니다.", MessageType.Warning);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
                if (GUILayout.Button("편집 가능한 UI 생성/연결")) TutorialSetup.BuildUI((TutorialSequence)target);
        }
        else if (GUILayout.Button("안내 패널 선택")) Selection.activeGameObject = panel;
    }
}
