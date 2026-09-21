using UnityEditor;
using UnityEngine;

/// <summary>직접 편집형 레이저 생성 및 기존 LineRenderer를 Undo 가능한 방식으로 전환합니다.</summary>
[CustomEditor(typeof(LaserHazard))]
public sealed class LaserHazardEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var laser = (LaserHazard)target;
        EditorGUILayout.HelpBox("Beam Collider에 직접 만든 BoxCollider2D를 연결하세요. 판정은 그 콜라이더만 사용하며 이미지 크기와 무관합니다. Size/Offset은 Edit Collider로 직접 조절하고 Is Trigger를 켜세요.", MessageType.Info);
        var collider = serializedObject.FindProperty("beamCollider").objectReferenceValue as BoxCollider2D;
        if (collider == null)
            EditorGUILayout.HelpBox("Beam Collider가 비어 있습니다. 자동 생성/검색하지 않으며 레이저 판정은 꺼집니다.", MessageType.Warning);
        else if (!collider.transform.IsChildOf(laser.transform) || !collider.isTrigger)
            EditorGUILayout.HelpBox("판정 콜라이더는 이 오브젝트 또는 자식이어야 하며 Is Trigger를 켜야 합니다. 설정 전에는 판정하지 않습니다.", MessageType.Warning);
        else if (collider.gameObject.layer != 2)
            EditorGUILayout.HelpBox("기존 투사체가 빔 판정에 부딪히지 않게 하려면 판정 오브젝트의 Layer를 Ignore Raycast로 지정하세요. 코드는 Layer를 자동 변경하지 않습니다.", MessageType.Info);
        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (laser.GetComponent<LineRenderer>() != null || laser.GetComponent<SpriteRenderer>() == null)
            {
                if (GUILayout.Button("기존 레이저를 SpriteRenderer로 전환"))
                    ConvertLaser(laser);
            }
        }
    }

    [MenuItem("GameObject/Story/Instant Death Laser", false, 11)]
    private static void CreateLaser(MenuCommand command)
    {
        var go = new GameObject("Instant Death Laser");
        GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
        go.AddComponent<LaserHazard>();
        Undo.RegisterCreatedObjectUndo(go, "Create Instant Death Laser");
        Selection.activeGameObject = go;
    }

    public static void ConvertLaser(LaserHazard laser)
    {
        if (laser == null || Application.isPlaying) return;
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Convert laser to editable SpriteRenderer");
        var data = new SerializedObject(laser);
        var line = laser.GetComponent<LineRenderer>();
        var renderer = data.FindProperty("beamRenderer").objectReferenceValue as SpriteRenderer;
        if (renderer == null) renderer = laser.GetComponent<SpriteRenderer>();
        // 같은 오브젝트에 필수 컴포넌트를 확보합니다. 이미 직접 설정된 이미지는 덮어쓰지 않습니다.
        if (laser.GetComponent<SpriteRenderer>() == null) Undo.AddComponent<SpriteRenderer>(laser.gameObject);
        if (renderer == null) renderer = laser.GetComponent<SpriteRenderer>();
        var oldSprite = data.FindProperty("beamSprite").objectReferenceValue as Sprite;
        if (renderer.sprite == null && oldSprite != null)
        {
            var visual = new GameObject("Beam Sprite");
            Undo.RegisterCreatedObjectUndo(visual, "Create editable beam sprite");
            visual.transform.SetParent(laser.transform, false);
            renderer = Undo.AddComponent<SpriteRenderer>(visual);
            renderer.sprite = oldSprite;
            renderer.color = data.FindProperty("spriteTint").colorValue;
            renderer.sortingOrder = line != null ? line.sortingOrder : 10;
            var end = data.FindProperty("endPoint").objectReferenceValue as Transform;
            Vector3 start = laser.transform.position;
            Vector3 finish = end != null ? end.position :
                start + laser.transform.right * Mathf.Max(.01f, data.FindProperty("length").floatValue);
            finish.z = start.z;
            Vector3 delta = finish - start;
            bool vertical = data.FindProperty("spriteIsVertical").boolValue;
            visual.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - (vertical ? 90f : 0f));
            float thickness = Mathf.Max(.01f, data.FindProperty("width").floatValue);
            Bounds bounds = oldSprite.bounds;
            float xSize = vertical ? thickness : delta.magnitude;
            float ySize = vertical ? delta.magnitude : thickness;
            Vector3 baseScale = visual.transform.lossyScale;
            visual.transform.localScale = new Vector3(
                xSize / Mathf.Max(.0001f, bounds.size.x * Mathf.Abs(baseScale.x)),
                ySize / Mathf.Max(.0001f, bounds.size.y * Mathf.Abs(baseScale.y)), 1);
            visual.transform.position = (start + finish) * .5f - visual.transform.TransformVector(bounds.center);
        }
        data.FindProperty("beamRenderer").objectReferenceValue = renderer;
        data.ApplyModifiedProperties();
        if (line != null) Undo.DestroyObjectImmediate(line);
        Undo.CollapseUndoOperations(undoGroup);
        EditorUtility.SetDirty(laser);
        Selection.activeGameObject = renderer.gameObject;
    }
}
