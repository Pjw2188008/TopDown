using UnityEditor;
using UnityEngine;

/// <summary>PlayerMove의 사이드뷰 설정을 표시합니다. 런타임에는 포함하지 않습니다.</summary>
[CustomEditor(typeof(PlayerMove)), CanEditMultipleObjects]
public sealed class PlayerMoveEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using(new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        EditorGUILayout.HelpBox("사이드뷰 전용: A/D 또는 좌우 방향키 이동, Space 점프, 왼쪽 Shift 대시. 공격/가드/패링은 좌우 클립을 사용합니다.", MessageType.Info);
        string[] hidden = { "m_Script" };
        DrawPropertiesExcluding(serializedObject,hidden);
        serializedObject.ApplyModifiedProperties();
    }
}
