using UnityEditor;
using UnityEngine;

/// <summary>PlayerMove의 공통 설정과 선택한 시점의 전용 설정만 표시합니다. 런타임에는 포함하지 않습니다.</summary>
[CustomEditor(typeof(PlayerMove)), CanEditMultipleObjects]
public sealed class PlayerMoveEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using(new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        var mode=serializedObject.FindProperty("sideViewMode");
        using(new EditorGUI.DisabledScope(Application.isPlaying))
            EditorGUILayout.PropertyField(mode,new GUIContent("사이드뷰 모드",mode.tooltip));
        bool mixed=mode.hasMultipleDifferentValues;
        bool side=!mixed && mode.boolValue;
        if(side)
        {
            EditorGUILayout.HelpBox("좌우 이동/전투 + 중력/점프. 공통 기능은 그대로 사용합니다. Space는 대시/달리기, 기본 점프는 W/↑입니다. 사이드뷰 Animator를 연결하고 PlayerMove만 붙이세요.",MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sideViewGravity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sideViewMaxFallSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sideViewJumpHeight"),new GUIContent("점프 높이"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sideViewJumpKey"),new GUIContent("점프 키"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sideViewAlternateJumpKey"),new GUIContent("추가 점프 키"));
        }
        string[] hidden=(side || mixed)
            ? new[]{"m_Script","sideViewMode","sideViewGravity","sideViewMaxFallSpeed","sideViewJumpHeight","sideViewJumpKey","sideViewAlternateJumpKey","verticalAttackHalfAngle"}
            : new[]{"m_Script","sideViewMode","sideViewGravity","sideViewMaxFallSpeed","sideViewJumpHeight","sideViewJumpKey","sideViewAlternateJumpKey"};
        DrawPropertiesExcluding(serializedObject,hidden);
        serializedObject.ApplyModifiedProperties();
    }
}
