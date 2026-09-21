using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

/// <summary>카메라 구역 생성 메뉴와 Scene 크기 조절 핸들입니다. 직접 부착하지 않습니다.</summary>
[CustomEditor(typeof(CameraBoundsArea))]
public sealed class CameraBoundsAreaEditor : Editor
{
    private readonly BoxBoundsHandle boundsHandle = new BoxBoundsHandle();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("Scene 뷰에서 사각형 핸들을 드래그해 맵 구역에 맞추세요. 물리 충돌은 생기지 않습니다. 구역 밖으로 플레이어가 나가도 이동 자체는 막지 않습니다.", MessageType.Info);
    }

    private void OnSceneGUI()
    {
        var area = (CameraBoundsArea)target;
        Bounds bounds = area.WorldBounds;
        boundsHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;
        boundsHandle.center = bounds.center;
        boundsHandle.size = bounds.size;
        EditorGUI.BeginChangeCheck();
        boundsHandle.DrawHandle();
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(area, "Resize Camera Bounds");
            serializedObject.Update();
            Vector3 localCenter = area.transform.InverseTransformPoint(boundsHandle.center);
            Vector3 scale = area.transform.lossyScale;
            serializedObject.FindProperty("center").vector2Value = localCenter;
            serializedObject.FindProperty("size").vector2Value = new Vector2(
                boundsHandle.size.x / Mathf.Max(.0001f, Mathf.Abs(scale.x)),
                boundsHandle.size.y / Mathf.Max(.0001f, Mathf.Abs(scale.y)));
            serializedObject.ApplyModifiedProperties();
        }
        Handles.Label(bounds.max, area.name + " (Priority " + area.Priority + ")");
    }

    [MenuItem("GameObject/Story/Camera Bounds Area", false, 10)]
    private static void CreateArea(MenuCommand command)
    {
        var go = new GameObject("Camera Bounds Area");
        GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
        go.AddComponent<CameraBoundsArea>();
        Undo.RegisterCreatedObjectUndo(go, "Create Camera Bounds Area");
        Selection.activeGameObject = go;
    }
}
