using CheolO.Monsters;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

/// <summary>Triangle 공격 시작 사각형의 중심/크기를 Scene에서 편집합니다. 오브젝트에 부착하지 않습니다.</summary>
[CustomEditor(typeof(TriangleMeleeMonster)), CanEditMultipleObjects]
public sealed class TriangleMeleeMonsterEditor : Editor
{
    private readonly BoxBoundsHandle areaHandle = new BoxBoundsHandle
    {
        axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y
    };

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("Scene의 청록색 중심 핸들: 위치 / 테두리 핸들: 가로·세로 크기. " +
            "공격 시작 영역이며 실제 피해 범위는 충격파 Radius입니다. 크기 0은 이전 정지 거리의 지름을 사용합니다.", MessageType.Info);
    }

    private void OnSceneGUI()
    {
        var monster = (TriangleMeleeMonster)target;
        Vector3 center = new Vector3(monster.AttackAreaCenter.x, monster.AttackAreaCenter.y, monster.transform.position.z);
        using (new Handles.DrawingScope(Color.cyan, Matrix4x4.identity))
        {
            EditorGUI.BeginChangeCheck();
            Vector3 moved = Handles.PositionHandle(center, Quaternion.identity);
            areaHandle.center = moved;
            areaHandle.size = new Vector3(monster.AttackAreaSize.x, monster.AttackAreaSize.y, 0f);
            areaHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(monster, "Edit Triangle Attack Area");
                monster.attackAreaOffset = (Vector2)(areaHandle.center - monster.transform.position);
                monster.attackAreaSize = new Vector2(Mathf.Max(.01f, areaHandle.size.x), Mathf.Max(.01f, areaHandle.size.y));
                EditorUtility.SetDirty(monster);
                if (PrefabUtility.IsPartOfPrefabInstance(monster))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(monster);
            }
        }
    }
}
