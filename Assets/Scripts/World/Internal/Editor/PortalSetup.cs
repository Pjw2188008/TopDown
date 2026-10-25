using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>포탈과 목적지 빈 오브젝트를 생성하는 편집 도구입니다. 직접 부착하지 않습니다.</summary>
public static class PortalSetup
{
    [MenuItem("GameObject/Story/Portal",false,10)]
    private static void CreatePortal(MenuCommand command)
    {
        var source=new GameObject("Portal");
        GameObjectUtility.SetParentAndAlign(source,command.context as GameObject);
        Undo.RegisterCreatedObjectUndo(source,"Create Portal");
        var component=Undo.AddComponent<Portal>(source);
        var destination=new GameObject("Portal Destination");
        SceneManager.MoveGameObjectToScene(destination,source.scene);
        destination.transform.position=source.transform.position+Vector3.right*5f;
        Undo.RegisterCreatedObjectUndo(destination,"Create Portal Destination");
        component.Destination=destination.transform;
        EditorUtility.SetDirty(component);
        Selection.activeGameObject=source;
    }
}
